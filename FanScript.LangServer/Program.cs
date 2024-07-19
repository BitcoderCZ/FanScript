using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using Serilog.Core;

namespace FanScript.LangServer
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            while (!Debugger.IsAttached)
            {
                await Task.Delay(1000);
            }

            //Log.Logger = new LoggerConfiguration()
            //            .Enrich.FromLogContext()
            //            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
            //            .MinimumLevel.Verbose()
            //            .CreateLogger();

            //Log.Information("Created logger");

            Console.OutputEncoding = new UTF8Encoding(); // UTF8N for non-Windows platform
            var app = new App(Console.OpenStandardInput(), Console.OpenStandardOutput());
            Logger.Instance.Attach(app);
            try
            {
                app.Listen().Wait();
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine(ex.InnerExceptions[0]);
                Environment.Exit(-1);
            }
        }
    }
}
