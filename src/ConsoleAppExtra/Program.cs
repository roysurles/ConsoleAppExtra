namespace ConsoleAppExtra;

// IoC
// Multiple appsettings.json
// Serilog
// parameter (args) checking

// Best Practices - https://benfoster.io/blog/serilog-best-practices/

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
        // https://github.com/serilog/serilog/wiki/Configuration-Basics

        // NOTE:  serilog only works in catch & finally blocks if next 3 lines are outside of try / catch / finnally
        // TODO:  investigate how to get serilog working w/o CreateHostBuilder so that we can log any exception that occurs in CreateHostBuilder
        /*
         * https://docs.datalust.co/docs/using-serilog
         * var auditLogger = new LoggerConfiguration()
                            .AuditTo.Seq("https://seq.example.com")
                            .CreateLogger();
         */
        using IHost host = CreateHostBuilder(args);
        Configuration = host.Services.GetRequiredService<IConfiguration>();
        CreateSerilogLogger();
        try
        {
            //using IHost host = CreateHostBuilder(args);
            //Configuration = host.Services.GetRequiredService<IConfiguration>();
            //CreateSerilogLogger();

            Log.Information("{V}", $"App Starting...{Environment.NewLine}");

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
            Log.Fatal(ex, "Exception occurred: ");
        }
        finally
        {
            Log.Information("{V}", $"App Exiting{Environment.NewLine}");
            Log.CloseAndFlush();
        }
    }

    static IHost CreateHostBuilder(string[] args)
    {
        // https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-usage
        return Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    //services.AddLogging(logging => logging.AddSerilog(dispose: true));
                    services.AddTransient<IWorkerService, WorkerService>();
                })
                .UseSerilog()
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

        /// TODO: push the templates to appsettings
        /// https://procodeguide.com/programming/aspnet-core-logging-with-serilog/
        const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}{Properties}";
        const string OutputTemplate1 = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Properties:j} {Message:lj}{NewLine}{Exception}";

        // https://rmauro.dev/setup-serilog-in-net6-as-logging-provider/
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(Configuration)
            .Enrich.WithMachineName()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: OutputTemplate1)
            .WriteTo.Debug(outputTemplate: OutputTemplate1)
            .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day, outputTemplate: OutputTemplate1)
            .CreateLogger();
    }
}