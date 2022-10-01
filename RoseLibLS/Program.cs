using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using RoseLibLS.LanguageServer;
using Serilog;
using System;
using System.Threading.Tasks;

namespace RoseLibLS
{
    class Program
    {
        private static void Main(string[] args) => MainAsync(args).Wait();

        private static async Task MainAsync(string[] args)
        {

            Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + "log_.txt", rollingInterval: RollingInterval.Day)
                        .MinimumLevel.Verbose()
                        .CreateLogger();

            Log.Logger.Debug("....Starting language server....");

            var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(
                options =>
                    options
                        .WithInput(Console.OpenStandardInput())
                        .WithOutput(Console.OpenStandardOutput())
                        .ConfigureLogging(
                                x => x
                                    .AddSerilog(Log.Logger)
                                    .AddLanguageProtocolLogging()
                                    .SetMinimumLevel(LogLevel.Trace)
                        )
                        .WithHandler<PCFGCommandHandler>()
                        .WithHandler<MCMCCommandHandler>()
                        .WithHandler<IdiomsCommandHandler>()
                        .WithHandler<PreviewCommandHandler>()
                        .WithHandler<GenerateCommandHandler>()
            ).ConfigureAwait(false);


            await server.WaitForExit.ConfigureAwait(false);
        }
    }
}
