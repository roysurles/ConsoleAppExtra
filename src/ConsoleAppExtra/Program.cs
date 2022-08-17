using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace ConsoleAppExtra;

// IoC
// Multiple appsettings.json
// Serilog
// parameter (args) checking

internal class Program
{

    static IHostEnvironment HostEnvironment { get; set; } = null!;

    /// <summary>
    /// https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration
    /// </summary>
    static IConfiguration Configuration { get; set; } = null!;

    static void Main(string[] args)
    {
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0
        try
        {
            using IHost host = CreateHostBuilder(args);
            Configuration = host.Services.GetRequiredService<IConfiguration>();
            CreateSerilogLogger();

            Log.Information("App Starting...");

            var programLogger = Log.Logger.ForContext<Program>();
            programLogger.Information("Log message for Program.cs");

            var environmentName = HostEnvironment.EnvironmentName;
            var appsettingsEnv = Configuration.GetValue<string>("Env");
            var commandLineAKey = Configuration.GetValue<string>("akey");  // Look at command line args

            // used to create a scope of services
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            var workerService = provider.GetRequiredService<IWorkerService>();
            workerService.DoWork1();

            //await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred: ");
        }
        finally
        {
            Log.Information("{V}", $"App Exiting...{Environment.NewLine}");
            Log.CloseAndFlush();
        }
    }

    static IHost CreateHostBuilder(string[] args)
    {
        // https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-usage
        return Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IWorkerService, WorkerService>();
                })
                .ConfigureAppConfiguration((hostingContext, configuration) =>
                {
                    configuration.Sources.Clear();

                    HostEnvironment = hostingContext.HostingEnvironment;
                    // env.ApplicationName = [custom application name]
                    // env.ContentRootPath = [custom content root path]
                    HostEnvironment.EnvironmentName = "Development";

                    // optional has to be false for serilog file sink to work
                    configuration
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{HostEnvironment.EnvironmentName}.json", false, true);

                    configuration.AddCommandLine(args);
                    // configuration.AddEnvironmentVariables    [add all machine level environment variables]
                    // configuration.AddInMemoryCollection      [add custom in-memory configuration]

                    IConfigurationRoot configurationRoot = configuration.Build();
                })
                .Build();
    }

    static void CreateSerilogLogger()
    {
        // https://nblumhardt.com/2021/06/customize-serilog-text-output/
        // https://github.com/serilog/serilog-settings-configuration
        //https://github.com/serilog/serilog-sinks-file

        // use separate config if needed around CreateHostBuilder
        //var slConfiguration = new ConfigurationBuilder()
        //    .SetBasePath(Directory.GetCurrentDirectory())
        //    .AddJsonFile(path: "appsettings.development.json", optional: false, reloadOnChange: true)
        //    .Build();

        /// push the templates to appsettings
        const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}{Properties}";
        const string OutputTemplate1 = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Properties:j} {Message:lj}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console(outputTemplate: OutputTemplate1)
            .WriteTo.Debug(outputTemplate: OutputTemplate1)
            .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day, outputTemplate: OutputTemplate1)
            .CreateLogger();
    }
}