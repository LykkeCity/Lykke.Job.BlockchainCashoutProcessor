using System.Collections.Generic;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Projections;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using CashinCompletedEvent = Lykke.Job.BlockchainCashoutProcessor.Contract.Events.CashinCompletedEvent;
using CashoutCompletedEvent = Lykke.Job.BlockchainCashoutProcessor.Contract.Events.CashoutCompletedEvent;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class CqrsModule : Module
    {
        public static readonly string Self = BlockchainCashoutProcessorBoundedContext.Name;

        private readonly CqrsSettings _settings;
        private readonly WorkflowSettings _workflowSettings;
        private readonly ILog _log;
        private readonly string _rabbitMqVirtualHost;

        public CqrsModule(
            CqrsSettings settings,
            WorkflowSettings workflowSettings,
            ILog log, 
            string rabbitMqVirtualHost = null)
        {
            _settings = settings;
            _workflowSettings = workflowSettings;
            _log = log;
            _rabbitMqVirtualHost = rabbitMqVirtualHost;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.RabbitConnectionString
            };

            var rabbitMqEndpoint = _rabbitMqVirtualHost == null
                ? rabbitMqSettings.Endpoint.ToString()
                : $"{rabbitMqSettings.Endpoint}/{_rabbitMqVirtualHost}";

            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "RabbitMq",
                        new TransportInfo(rabbitMqEndpoint, rabbitMqSettings.UserName,
                            rabbitMqSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory());

            // Sagas
            builder.RegisterType<CashoutSaga>();
            builder.RegisterType<CrossClientCashoutSaga>();
            builder.RegisterType<CashoutBatchSaga>();

            // Command handlers
            builder.RegisterType<StartCashoutCommandsHandler>()
                .WithParameter(TypedParameter.From(_workflowSettings?.DisableDirectCrossClientCashouts ?? false));
            builder.RegisterType<EnrollToMatchingEngineCommandsHandler>();
            builder.RegisterType<MatchingEngineCallDeduplicationsProjection>();
            builder.RegisterType<OperationCompletedCommandsHandler>();
            builder.RegisterType<NotifyCashoutFailedCommandsHandler>();
            builder.RegisterType<DeleteActiveBatchCommandHandler>();
            builder.RegisterType<NotifyBatchFailedCommandHandler>();
            builder.RegisterType<NotifyBatchCompetedCommandHandler>();
            builder.RegisterType<SuspendActiveBatchCommandHandler>();
            builder.RegisterType<StartBatchExecutionCommandHandler>();
            builder.RegisterType<AddOperationToBatchCommandHandler>();

            // Projections
            builder.RegisterType<ClientOperationsProjection>();

            builder.Register(ctx => CreateEngine(ctx, messagingEngine))
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }

        private CqrsEngine CreateEngine(IComponentContext ctx, IMessagingEngine messagingEngine)
        {
            var defaultRetryDelay = (long)_settings.RetryDelay.TotalMilliseconds;

            const string defaultPipeline = "commands";
            const string defaultRoute = "self";
            const string eventsRoute = "events";

            return new CqrsEngine(
                _log,
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

                    .ListeningCommands(typeof(NotifyCashinCompletedCommand),
                        typeof(NotifyCashoutCompletedCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<OperationCompletedCommandsHandler>()
                    .PublishingEvents(typeof(CashoutCompletedEvent),
                                      typeof(CashinCompletedEvent))
                    .With(eventsRoute)

                    .ListeningCommands(typeof(NotifyCashoutFailedCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<NotifyCashoutFailedCommandsHandler>()
                    .PublishingEvents(typeof(CashoutFailedEvent))
                    .With(eventsRoute)


                    .ListeningCommands(typeof(StartCashoutCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<StartCashoutCommandsHandler>()
                    .PublishingEvents(
                        typeof(CashoutStartedEvent),
                        typeof(CrossClientCashoutStartedEvent),
                        typeof(CashoutBatchingStartedEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(EnrollToMatchingEngineCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<EnrollToMatchingEngineCommandsHandler>()
                    .PublishingEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On("client-operations")
                    .WithProjection(typeof(ClientOperationsProjection), BlockchainOperationsExecutorBoundedContext.Name)

                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(Self)
                    .On("client-operations")
                    .WithProjection(typeof(ClientOperationsProjection), Self)

                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(Self)
                    .On(eventsRoute)
                    .WithProjection(typeof(MatchingEngineCallDeduplicationsProjection), Self)

                    .ListeningCommands(typeof(NotifyBatchFailedCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<NotifyBatchFailedCommandHandler>()
                    .PublishingEvents(typeof(BatchedOperationExecutionFailedEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(NotifyBatchCompletedCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<NotifyBatchCompetedCommandHandler>()
                    .PublishingEvents(typeof(BatchedOperationExecutionCompletedEvent))
                    .With(defaultPipeline)

                    .ProcessingOptions(defaultRoute).MultiThreaded(4).QueueCapacity(1024)
                    .ProcessingOptions(eventsRoute).MultiThreaded(4).QueueCapacity(1024)
                    .ProcessingOptions("client-operations").MultiThreaded(4).QueueCapacity(1024),


                Register.BoundedContext(BlockchainCashoutBatchProcessorBoundedContext.Name)
                    .FailedCommandRetryDelay(defaultRetryDelay)

                    .ListeningCommands(typeof(StartBatchExecutionCommand))
                    .On(defaultRoute)
                    .WithLoopback()
                    .WithCommandsHandler<StartBatchExecutionCommandHandler>()
                    .PublishingEvents(typeof(BatchExecutionStartedEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(AddOperationToBatchCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<AddOperationToBatchCommandHandler>()

                    .ListeningCommands(typeof(SuspendActiveBatchCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<SuspendActiveBatchCommandHandler>()
                    .PublishingEvents(typeof(BatchSuspendedEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(DeleteActiveBatchCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<DeleteActiveBatchCommandHandler>()

                    .ProcessingOptions(defaultRoute).MultiThreaded(4).QueueCapacity(1024)
                    .ProcessingOptions(eventsRoute).MultiThreaded(4).QueueCapacity(1024),

                Register.Saga<CashoutSaga>($"{Self}.saga")
                    .ListeningEvents(typeof(CashoutStartedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(
                        typeof(BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand))
                    .To(BlockchainOperationsExecutorBoundedContext.Name)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(CashoutBatchingStartedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(
                        typeof(AddOperationToBatchCommand))
                    .To(BlockchainCashoutBatchProcessorBoundedContext.Name)
                    .With(defaultPipeline)

                    .ListeningEvents(
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent),
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(NotifyCashoutCompletedCommand),
                        typeof(NotifyCashoutFailedCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(
                        typeof(BatchedOperationExecutionCompletedEvent),
                        typeof(BatchedOperationExecutionFailedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(NotifyCashoutCompletedCommand))
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
                    .PublishingCommands(typeof(NotifyCashinCompletedCommand),
                                        typeof(NotifyCashoutCompletedCommand))
                    .To(Self)
                    .With(defaultPipeline),

                 Register.Saga<CashoutBatchSaga>($"{Self}.batch-saga")
                     .ListeningEvents(typeof(BatchExecutionStartedEvent))
                     .From(BlockchainOperationsExecutorBoundedContext.Name)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(SuspendActiveBatchCommand))
                     .To(BlockchainOperationsExecutorBoundedContext.Name)
                     .With(defaultPipeline)
                     
                     .ListeningEvents(typeof(BatchSuspendedEvent))
                     .From(BlockchainOperationsExecutorBoundedContext.Name)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand))
                     .To(BlockchainOperationsExecutorBoundedContext.Name)
                     .With(defaultPipeline)
                     .PublishingCommands(typeof(BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand))
                     .To(BlockchainOperationsExecutorBoundedContext.Name)
                     .With(defaultPipeline)

                     .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent))
                     .From(BlockchainOperationsExecutorBoundedContext.Name)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(NotifyBatchCompletedCommand))
                     .To(BlockchainCashoutProcessorBoundedContext.Name)
                     .With(defaultPipeline)

                     .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent))
                     .From(BlockchainOperationsExecutorBoundedContext.Name)
                     .On(defaultRoute)
                     .PublishingCommands(typeof(NotifyBatchFailedCommand))
                     .To(BlockchainCashoutProcessorBoundedContext.Name)
                     .With(defaultPipeline)
                );
        }
    }
}
