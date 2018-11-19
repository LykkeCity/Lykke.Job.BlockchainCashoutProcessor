using System.Collections.Generic;
using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Regular;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Regular;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Projections;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class CqrsModule : Module
    {
        public static readonly string Self = BlockchainCashoutProcessorBoundedContext.Name;

        private readonly CqrsSettings _settings;
        private readonly WorkflowSettings _workflowSettings;
        private readonly BatchingSettings _batchingSettings;
        private readonly string _rabbitMqVirtualHost;

        public CqrsModule(
            CqrsSettings settings,
            WorkflowSettings workflowSettings,
            BatchingSettings batchingSettings,
            string rabbitMqVirtualHost = null)
        {
            _settings = settings;
            _workflowSettings = workflowSettings;
            _batchingSettings = batchingSettings;
            _rabbitMqVirtualHost = rabbitMqVirtualHost;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();
            builder.RegisterInstance(_settings);

            // Sagas
            builder.RegisterType<CashoutSaga>();
            builder.RegisterType<CrossClientCashoutSaga>();
            builder.RegisterType<BatchSaga>();

            // Common command handlers
            builder.RegisterType<StartCashoutCommandsHandler>()
                .WithParameter(TypedParameter.From(_workflowSettings?.DisableDirectCrossClientCashouts ?? false));

            // Batch command handlers
            builder.RegisterType<CloseBatchCommandsHandler>();
            builder.RegisterType<CompleteBatchCommandsHandler>();
            builder.RegisterType<FailBatchCommandsHandler>();
            builder.RegisterType<RevokeActiveBatchIdCommandsHandler>();
            builder.RegisterType<WaitForBatchExpirationCommandsHandler>()
                .WithParameter(TypedParameter.From(_batchingSettings.ExpirationMonitoringPeriod));
            
            // Cross client command handlers
            builder.RegisterType<EnrollToMatchingEngineCommandsHandler>();
            builder.RegisterType<NotifyCrossClientCashoutCompletedCommandsHandler>();

            // Regular command handlers
            builder.RegisterType<NotifyCashoutCompletedCommandsHandler>();
            builder.RegisterType<NotifyCashoutFailedCommandsHandler>();

            // Projections
            builder.RegisterType<MatchingEngineCallDeduplicationsProjection>();
           
            builder.Register(CreateEngine)
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }

        private CqrsEngine CreateEngine(IComponentContext ctx)
        {
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.RabbitConnectionString
            };

            var rabbitMqEndpoint = _rabbitMqVirtualHost == null
                ? rabbitMqSettings.Endpoint.ToString()
                : $"{rabbitMqSettings.Endpoint}/{_rabbitMqVirtualHost}";

            var logFactory = ctx.Resolve<ILogFactory>();

            var messagingEngine = new MessagingEngine(
                logFactory,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "RabbitMq",
                        new TransportInfo(rabbitMqEndpoint, rabbitMqSettings.UserName,
                            rabbitMqSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory(logFactory));

            var defaultRetryDelay = (long)_settings.RetryDelay.TotalMilliseconds;

            const string defaultPipeline = "commands";
            const string defaultRoute = "self";
            const string eventsRoute = "events";

            return new CqrsEngine(
                logFactory,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                    "RabbitMq",
                    SerializationFormat.MessagePack,
                    environment: "lykke")),

                Register.BoundedContext(Self)
                    .FailedCommandRetryDelay(defaultRetryDelay)
                    .ProcessingOptions(defaultRoute).MultiThreaded(4).QueueCapacity(1024)
                    .ProcessingOptions(eventsRoute).MultiThreaded(4).QueueCapacity(1024)

                    // Common

                    .ListeningCommands(typeof(StartCashoutCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<StartCashoutCommandsHandler>()
                    .PublishingEvents(
                        typeof(CashoutStartedEvent),
                        typeof(CrossClientCashoutStartedEvent),
                        typeof(BatchFillingStartedEvent),
                        typeof(BatchFilledEvent),
                        typeof(BatchedCashoutStartedEvent))
                    .With(defaultPipeline)

                    // Regular

                    .ListeningCommands(typeof(NotifyCashoutCompletedCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<NotifyCashoutCompletedCommandsHandler>()
                    .PublishingEvents(typeof(CashoutCompletedEvent))
                    .With(eventsRoute)

                    .ListeningCommands(typeof(NotifyCashoutFailedCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<NotifyCashoutFailedCommandsHandler>()
                    .PublishingEvents(typeof(CashoutFailedEvent))
                    .With(eventsRoute)

                    // Cross client

                    .ListeningCommands(typeof(EnrollToMatchingEngineCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<EnrollToMatchingEngineCommandsHandler>()
                    .PublishingEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(NotifyCrossClientCashoutCompletedCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<NotifyCrossClientCashoutCompletedCommandsHandler>()
                    .PublishingEvents(typeof(CrossClientCashoutCompletedEvent))
                    .With(eventsRoute)

                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(Self)
                    .On(eventsRoute)
                    .WithProjection(typeof(MatchingEngineCallDeduplicationsProjection), Self)

                    // Batching

                    .ListeningCommands(typeof(CloseBatchCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<CloseBatchCommandsHandler>()
                    .PublishingEvents(typeof(BatchClosedEvent))
                    .With(eventsRoute)

                    .ListeningCommands(typeof(CompleteBatchCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<CompleteBatchCommandsHandler>()
                    .PublishingEvents(typeof(CashoutsBatchCompletedEvent))
                    .With(eventsRoute)

                    .ListeningCommands(typeof(FailBatchCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<FailBatchCommandsHandler>()
                    .PublishingEvents(typeof(CashoutsBatchFailedEvent))
                    .With(eventsRoute)

                    .ListeningCommands(typeof(RevokeActiveBatchIdCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<RevokeActiveBatchIdCommandsHandler>()
                    .PublishingEvents(typeof(ActiveBatchIdRevokedEvent))
                    .With(eventsRoute)

                    .ListeningCommands(typeof(WaitForBatchExpirationCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<WaitForBatchExpirationCommandsHandler>()
                    .PublishingEvents(typeof(BatchExpiredEvent))
                    .With(eventsRoute),
                    
                Register.Saga<CashoutSaga>($"{Self}.saga")
                    .ListeningEvents(typeof(CashoutStartedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand))
                    .To(BlockchainOperationsExecutorBoundedContext.Name)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(NotifyCashoutCompletedCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(NotifyCashoutFailedCommand))
                    .To(Self)
                    .With(defaultPipeline),

                 Register.Saga<CrossClientCashoutSaga>($"{Self}.cross-client-saga")
                    .ListeningEvents(typeof(CrossClientCashoutStartedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(EnrollToMatchingEngineCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(NotifyCrossClientCashoutCompletedCommand))
                    .To(Self)
                    .With(defaultPipeline),

                 Register.Saga<BatchSaga>($"{Self}.batch-saga")
                     .ListeningEvents(typeof(BatchFillingStartedEvent))
                     .From(Self)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(WaitForBatchExpirationCommand))
                     .To(Self)
                     .With(defaultPipeline)

                     .ListeningEvents(typeof(BatchFilledEvent))
                     .From(Self)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(CloseBatchCommand))
                     .To(Self)
                     .With(defaultPipeline)

                     .ListeningEvents(typeof(BatchExpiredEvent))
                     .From(Self)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(CloseBatchCommand))
                     .To(Self)
                     .With(defaultPipeline)

                     .ListeningEvents(typeof(BatchClosedEvent))
                     .From(Self)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(RevokeActiveBatchIdCommand))
                     .To(Self)
                     .With(defaultPipeline)

                     .ListeningEvents(typeof(ActiveBatchIdRevokedEvent))
                     .From(Self)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand))
                     .To(BlockchainOperationsExecutorBoundedContext.Name)
                     .With(defaultPipeline)

                     .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OneToManyOperationExecutionCompletedEvent))
                     .From(BlockchainOperationsExecutorBoundedContext.Name)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(CompleteBatchCommand))
                     .To(Self)
                     .With(defaultPipeline)

                     .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent))
                     .From(BlockchainOperationsExecutorBoundedContext.Name)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(FailBatchCommand))
                     .To(Self)
                     .With(defaultPipeline)
                );
        }
    }
}
