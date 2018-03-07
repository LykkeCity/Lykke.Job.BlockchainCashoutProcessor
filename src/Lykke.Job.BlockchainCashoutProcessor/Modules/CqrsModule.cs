using System.Collections.Generic;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
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

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class CqrsModule : Module
    {
        public static readonly string Self = BlockchainCashoutProcessorBoundedContext.Name;

        private readonly CqrsSettings _settings;
        private readonly WorkflowSettings _workflowSettings;
        private readonly ILog _log;

        public CqrsModule(
            CqrsSettings settings, 
            WorkflowSettings workflowSettings, 
            ILog log)
        {
            _settings = settings;
            _workflowSettings = workflowSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.RabbitConnectionString
            };
            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "RabbitMq",
                        new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName,
                            rabbitMqSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory());

            // Sagas
            builder.RegisterType<CashoutSaga>();
            builder.RegisterType<CrossClientCashoutSaga>();
            
            // Command handlers
            builder.RegisterType<StartCashoutCommandsHandler>()
                .WithParameter(TypedParameter.From(_workflowSettings?.DisableDirectCrossClientCashouts ?? false));
            builder.RegisterType<EnrollToMatchingEngineCommandsHandler>();
            builder.RegisterType<RegisterClientOperationFinishCommandsHandler>();
            builder.RegisterType<MatchingEngineCallDeduplicationsProjection>();

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
            const string eventsRoute = "evets";

            return new CqrsEngine(
                _log,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                    "RabbitMq",
                    "messagepack",
                    environment: "lykke")),

                Register.BoundedContext(Self)
                    .FailedCommandRetryDelay(defaultRetryDelay)

                    .ListeningCommands(typeof(StartCashoutCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<StartCashoutCommandsHandler>()
                    .PublishingEvents(
                        typeof(CashoutStartedEvent),
                        typeof(CrossClientCashoutStartedEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(RegisterClientOperationFinishCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<RegisterClientOperationFinishCommandsHandler>()
                    .PublishingEvents(typeof(ClientOperationFinishRegisteredEvent))
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

                    .ProcessingOptions(defaultRoute).MultiThreaded(4).QueueCapacity(1024)
                    .ProcessingOptions(eventsRoute).MultiThreaded(4).QueueCapacity(1024)
                    .ProcessingOptions("client-operations").MultiThreaded(4).QueueCapacity(1024),

                Register.Saga<CashoutSaga>($"{Self}.saga")
                    .ListeningEvents(
                        typeof(CashoutStartedEvent)
                        )
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(
                        typeof(BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand))
                    .To(BlockchainOperationsExecutorBoundedContext.Name)
                    .With(defaultPipeline)

                    .ListeningEvents(
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent),
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(defaultRoute)

                    .ListeningEvents(typeof(ClientOperationFinishRegisteredEvent))
                    .From(Self)
                    .On(defaultRoute),

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
                );
        }
    }
}
