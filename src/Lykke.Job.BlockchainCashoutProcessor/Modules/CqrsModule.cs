﻿using System.Collections.Generic;
using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.MessageCancellation.Interceptors;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Regular;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.RiskControl;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Regular;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.RiskControl;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.RiskControl;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Interceptors;
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
            builder.RegisterType<RiskControlSaga>();

            // Common command handlers
            builder.RegisterType<StartCashoutCommandsHandler>();

            // Risk control command handlers
            builder.RegisterType<AcceptCashoutCommandsHandler>()
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

            // Interceptors
            builder.RegisterType<ErrorsCommandInterceptor>();
            builder.RegisterType<ErrorsEventInterceptor>();

            //CQRS Message Cancellation
            Lykke.Cqrs.MessageCancellation.Configuration.ContainerBuilderExtensions.RegisterCqrsMessageCancellation(
                builder,
                (options) =>
                {
                    #region Registry

                    //Commands
                    options.Value
                        .MapMessageId<EnrollToMatchingEngineCommand>(x => x.CashinOperationId.ToString())
                        .MapMessageId<CloseBatchCommand>(x => x.BatchId.ToString())
                        .MapMessageId<CompleteBatchCommand>(x => x.BatchId.ToString())
                        .MapMessageId<FailBatchCommand>(x => x.BatchId.ToString())
                        .MapMessageId<RevokeActiveBatchIdCommand>(x => x.BatchId.ToString())
                        .MapMessageId<WaitForBatchExpirationCommand>(x => x.BatchId.ToString())
                        .MapMessageId<NotifyCrossClientCashoutCompletedCommand>(x => x.OperationId.ToString())
                        .MapMessageId<NotifyCashoutCompletedCommand>(x => x.OperationId.ToString())
                        .MapMessageId<NotifyCashoutFailedCommand>(x => x.OperationId.ToString())
                        .MapMessageId<StartCashoutCommand>(x => x.OperationId.ToString())
                        .MapMessageId<AcceptCashoutCommand>(x => x.OperationId.ToString())

                        //Events
                        .MapMessageId<BatchedCashoutStartedEvent>(x => x.OperationId.ToString())
                        .MapMessageId<CashoutCompletedEvent>(x => x.OperationId.ToString())
                        .MapMessageId<CashoutFailedEvent>(x => x.OperationId.ToString())
                        .MapMessageId<CashoutsBatchCompletedEvent>(x => x.BatchId.ToString())
                        .MapMessageId<CashoutsBatchFailedEvent>(x => x.BatchId.ToString())
                        .MapMessageId<CashoutStartedEvent>(x => x.OperationId.ToString())
                        .MapMessageId<CrossClientCashoutCompletedEvent>(x => x.OperationId.ToString())
                        .MapMessageId<CrossClientCashoutStartedEvent>(x => x.OperationId.ToString())
                        .MapMessageId<BatchedCashout>(x => x.OperationId.ToString())
                        .MapMessageId<ValidationStartedEvent>(x => x.OperationId.ToString())

                        .MapMessageId<CashinEnrolledToMatchingEngineEvent>(x => x.CashoutOperationId.ToString())
                        .MapMessageId<ActiveBatchIdRevokedEvent>(x => x.BatchId.ToString())
                        .MapMessageId<BatchClosedEvent>(x => x.BatchId.ToString())
                        .MapMessageId<BatchExpiredEvent>(x => x.BatchId.ToString())
                        .MapMessageId<BatchFilledEvent>(x => x.BatchId.ToString())
                        .MapMessageId<BatchFillingStartedEvent>(x => x.BatchId.ToString())

                        //External Commands
                        .MapMessageId<BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand>(
                            x => x.OperationId.ToString())
                        .MapMessageId<BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand>(
                            x => x.OperationId.ToString())
                        .MapMessageId<BlockchainRiskControl.Contract.Commands.ValidateOperationCommand>(
                            x => x.OperationId.ToString())

                        //External Events
                        .MapMessageId<BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent>(
                            x => x.OperationId.ToString())
                        .MapMessageId<BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent>(
                            x => x.OperationId.ToString())
                        .MapMessageId<BlockchainOperationsExecutor.Contract.Events.OneToManyOperationExecutionCompletedEvent>(
                            x => x.OperationId.ToString())
                        .MapMessageId<BlockchainRiskControl.Contract.Events.OperationAcceptedEvent>(
                            x => x.OperationId.ToString())
                        .MapMessageId<BlockchainRiskControl.Contract.Events.OperationRejectedEvent>(
                            x => x.OperationId.ToString());

                    #endregion
                });

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
                Register.CommandInterceptor<ErrorsCommandInterceptor>(),
                Register.EventInterceptor<ErrorsEventInterceptor>(),
            #region CQRS Message Cancellation
                Register.CommandInterceptor<MessageCancellationCommandInterceptor>(),
                Register.EventInterceptor<MessageCancellationEventInterceptor>(),
            #endregion
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
                    .PublishingEvents(typeof(ValidationStartedEvent))
                    .With(defaultPipeline)

                    // Risk control

                    .ListeningCommands(typeof(AcceptCashoutCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<AcceptCashoutCommandsHandler>()
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

                Register.Saga<RiskControlSaga>($"{Self}.risk-control-saga")
                    .ListeningEvents(typeof(ValidationStartedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(BlockchainRiskControl.Contract.Commands.ValidateOperationCommand))
                    .To(BlockchainRiskControl.Contract.BlockchainRiskControlBoundedContext.Name)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(BlockchainRiskControl.Contract.Events.OperationAcceptedEvent))
                    .From(BlockchainRiskControl.Contract.BlockchainRiskControlBoundedContext.Name)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(AcceptCashoutCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(BlockchainRiskControl.Contract.Events.OperationRejectedEvent))
                    .From(BlockchainRiskControl.Contract.BlockchainRiskControlBoundedContext.Name)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(NotifyCashoutFailedCommand))
                    .To(Self)
                    .With(defaultPipeline),

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
