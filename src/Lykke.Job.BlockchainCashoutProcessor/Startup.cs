using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AsyncFriendlyStackTrace;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.AppServices.Lifecycle;
using Lykke.Job.BlockchainCashoutProcessor.Modules;
using Lykke.Job.BlockchainCashoutProcessor.Settings;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeSlack;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.BlockchainCashoutProcessor
{
    public class Startup
    {
        private IContainer ApplicationContainer { get; set; }
        private IConfigurationRoot Configuration { get; }
        private ILog Log { get; set; }
        private IHealthNotifier HealthNotifier { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddMvc()
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.ContractResolver =
                            new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "BlockchainCashoutProcessor API");
                });

                var appSettings = Configuration.LoadSettings<AppSettings>(options =>
                {
                    options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                    options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                    options.SenderName = "Lykke.Job.BlockchainCashoutProcessor";
                });

                services.AddLykkeLogging(
                    appSettings.Nested(x => x.BlockchainCashoutProcessorJob.Db.LogsConnString),
                    "BlockchainCashoutProcessorLog",
                    appSettings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                    appSettings.CurrentValue.SlackNotifications.AzureQueue.QueueName,
                    options =>
                    {
                        options.AddAdditionalSlackChannel(
                            "CommonBlockChainIntegration",
                            slackOptions =>
                            {
                                slackOptions.MinLogLevel = Microsoft.Extensions.Logging.LogLevel.Information;
                                slackOptions.IncludeHealthNotifications();
                                slackOptions.SpamGuard.DisableGuarding();
                            });

                        options.AddAdditionalSlackChannel(
                            "CommonBlockChainIntegrationImportantMessages",
                            slackOptions =>
                            {
                                slackOptions.MinLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
                                slackOptions.IncludeHealthNotifications();
                                slackOptions.SpamGuard.DisableGuarding();
                            });
                    });

                var builder = new ContainerBuilder();

                builder.RegisterModule(new JobModule(
                    appSettings.CurrentValue.Assets,
                    appSettings.CurrentValue.BlockchainCashoutProcessorJob.ChaosKitty,
                    appSettings.CurrentValue.MatchingEngineClient));
                builder.RegisterModule(new RepositoriesModule(
                    appSettings.Nested(x => x.BlockchainCashoutProcessorJob.Db)));
                builder.RegisterModule(new BlockchainsModule(
                    appSettings.CurrentValue.BlockchainsIntegration,
                    appSettings.CurrentValue.BlockchainWalletsServiceClient));
                builder.RegisterModule(new CqrsModule(
                    appSettings.CurrentValue.BlockchainCashoutProcessorJob.Cqrs,
                    appSettings.CurrentValue.BlockchainCashoutProcessorJob.Workflow,
                    appSettings.CurrentValue.BlockchainCashoutProcessorJob.Batching));

                services.AddHttpClient(HttpClientNames.Opsgenie, client =>
                {
                    if (appSettings.CurrentValue.Opsgenie == null)
                        return;

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GenieKey", appSettings.CurrentValue.Opsgenie.ApiKey);
                    client.BaseAddress = new Uri(appSettings.CurrentValue.Opsgenie.ServiceUrl);
                });

                builder.Populate(services);

                ApplicationContainer = builder.Build();

                Log = ApplicationContainer.Resolve<ILogFactory>().CreateLog(this);
                HealthNotifier = ApplicationContainer.Resolve<IHealthNotifier>();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseLykkeMiddleware(ex => ErrorResponse.Create(ex.ToAsyncString()));

                app.UseMvc();
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
                });
                app.UseSwaggerUI(x =>
                {
                    x.RoutePrefix = "swagger/ui";
                    x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                });
                app.UseStaticFiles();

                appLifetime.ApplicationStarted.Register(() => StartApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopping.Register(() => StopApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopped.Register(CleanUp);
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        private async Task StartApplication()
        {
            try
            {
                // NOTE: Job not yet recieve and process IsAlive requests here

                await ApplicationContainer.Resolve<IStartupManager>().StartAsync();

                HealthNotifier?.Notify("Started");
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                // NOTE: Job still can recieve and process IsAlive requests here, so take care about it if you add logic here.

                await ApplicationContainer.Resolve<IShutdownManager>().StopAsync();
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        private void CleanUp()
        {
            try
            {
                // NOTE: Job can't recieve and process IsAlive requests here, so you can destroy all resources

                HealthNotifier?.Notify("Terminating");

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }
    }
}
