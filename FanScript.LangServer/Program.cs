using FanScript.LangServer.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using System;
using System.Globalization;
using System.Threading.Tasks;
#if DEBUG
using System.Diagnostics;
#endif

namespace FanScript.LangServer;

internal class Program
{
    private static async Task Main(string[] args)
    {
#if DEBUG
        Debugger.Launch();
#endif

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                    .MinimumLevel.Verbose()
                    .CreateLogger();

        Log.Logger.Information("This only goes file...");

        //IObserver<WorkDoneProgressReport> workDone = null!;

        var server = await LanguageServer.From(
            options =>
                options
                   .WithInput(Console.OpenStandardInput())
                   .WithOutput(Console.OpenStandardOutput())
                   .ConfigureLogging(
                        x => x
                            .AddSerilog(Log.Logger)
                            .AddLanguageProtocolLogging()
                            .SetMinimumLevel(LogLevel.Debug)
                    )
                   .WithServices(
                        services => services
                            .AddSingleton<TextDocumentHandler>()
                    )
                   .WithHandler<CompletionHandler>()
                   .WithHandler<HoverHandler>()
                   .WithHandler<DidChangeWatchedFilesHandler>()
                   .WithHandler<FoldingRangeHandler>()
                   .WithHandler<MyWorkspaceSymbolsHandler>()
                   .WithHandler<MyDocumentSymbolHandler>()
                   .WithHandler<SemanticTokensHandler>()
                   .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))
                   .WithServices(
                        services =>
                        {
                            services.AddSingleton(
                                provider =>
                                {
                                    var loggerFactory = provider.GetService<ILoggerFactory>()!;
                                    var logger = loggerFactory.CreateLogger<Program>();

                                    logger.LogInformation("Configuring");

                                    return logger;
                                }
                            );
                            services.AddSingleton(
                                new ConfigurationItem
                                {
                                    Section = "fanscript",
                                }
                            );
                        }
                    )
                   //.OnInitialize(
                   //     async (server, request, token) =>
                   //     {
                   //         var manager = server.WorkDoneManager.For(
                   //             request, new WorkDoneProgressBegin
                   //             {
                   //                 Title = "Server is starting...",
                   //                 Percentage = 10,
                   //             }
                   //         );
                   //         workDone = manager;

                   //         await Task.Delay(2000).ConfigureAwait(false);

                   //         manager.OnNext(
                   //             new WorkDoneProgressReport
                   //             {
                   //                 Percentage = 20,
                   //                 Message = "loading in progress"
                   //             }
                   //         );
                   //     }
                   // )
                   //.OnInitialized(
                   //     async (server, request, response, token) =>
                   //     {
                   //         workDone.OnNext(
                   //             new WorkDoneProgressReport
                   //             {
                   //                 Percentage = 40,
                   //                 Message = "loading almost done",
                   //             }
                   //         );

                   //         await Task.Delay(2000).ConfigureAwait(false);

                   //         workDone.OnNext(
                   //             new WorkDoneProgressReport
                   //             {
                   //                 Message = "loading done",
                   //                 Percentage = 100,
                   //             }
                   //         );
                   //         workDone.OnCompleted();
                   //     }
                   // )
                   .OnStarted(
                        async (languageServer, token) =>
                        {
                            //using var manager = await languageServer.WorkDoneManager.Create(new WorkDoneProgressBegin { Title = "Doing some work..." })
                            //                                        .ConfigureAwait(false);

                            //manager.OnNext(new WorkDoneProgressReport { Message = "doing things..." });
                            //await Task.Delay(2000).ConfigureAwait(false);
                            //manager.OnNext(new WorkDoneProgressReport { Message = "doing things... 1234" });
                            //await Task.Delay(2000).ConfigureAwait(false);
                            //manager.OnNext(new WorkDoneProgressReport { Message = "doing things... 56789" });

                            var logger = languageServer.Services.GetService<ILogger<Program>>()!;
                            var configuration = await languageServer.Configuration.GetConfiguration(
                                new ConfigurationItem
                                {
                                    Section = "fanscript",
                                }
                            ).ConfigureAwait(false);

                            var baseConfig = new JObject();
                            foreach (var config in languageServer.Configuration.AsEnumerable())
                                baseConfig.Add(config.Key, config.Value);

                            logger.LogInformation("Base Config: {@Config}", baseConfig);

                            var scopedConfig = new JObject();
                            foreach (var config in configuration.AsEnumerable())
                                scopedConfig.Add(config.Key, config.Value);

                            logger.LogInformation("Scoped Config: {@Config}", scopedConfig);
                        }
                    )
        ).ConfigureAwait(false);

        await server.WaitForExit.ConfigureAwait(false);
    }
}
