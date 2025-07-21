using DataCaptureService.Services;
using ImageFlow.Shared.Interfaces;
using ImageFlow.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DataCaptureService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/datacapture-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting Data Capture Service");

                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Data Capture Service terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                        optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    services.AddSingleton<RetryPolicy>();
                    services.AddSingleton<IFileChunkSender>(serviceProvider =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<FileChunkSender>>();
                        var config = serviceProvider.GetRequiredService<IConfiguration>();
                        
                        return FileChunkSender.CreateAsync(logger, config).Result;
                    });

                    services.AddSingleton<IFileWatcher>(serviceProvider =>
                    {
                        var config = serviceProvider.GetRequiredService<IConfiguration>();
                        var sender = serviceProvider.GetRequiredService<IFileChunkSender>();
                        var logger = serviceProvider.GetRequiredService<ILogger<FileWatcherService>>();
                        var retryPolicy = serviceProvider.GetRequiredService<RetryPolicy>();

                        var inputFolder = config["Processing:InputFolder"] ?? "InputImages";
                        return new FileWatcherService(inputFolder, sender, logger, config, retryPolicy);
                    });

                    services.AddHostedService<DataCaptureWorker>();
                });
    }

    public class DataCaptureWorker : BackgroundService
    {
        private readonly IFileWatcher _fileWatcher;
        private readonly ILogger<DataCaptureWorker> _logger;
        private readonly IConfiguration _configuration;

        public DataCaptureWorker(
            IFileWatcher fileWatcher, 
            ILogger<DataCaptureWorker> logger,
            IConfiguration configuration)
        {
            _fileWatcher = fileWatcher;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var inputFolder = _configuration["Processing:InputFolder"] ?? "InputImages";
                
                Directory.CreateDirectory(inputFolder);

                _fileWatcher.FileDetected += OnFileDetected;

                await _fileWatcher.StartAsync(stoppingToken);

                _logger.LogInformation("Data Capture Service is running and watching folder: {InputFolder}", 
                    inputFolder);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Data Capture Service is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Data Capture Worker");
                throw;
            }
        }

        private Task OnFileDetected(string filePath)
        {
            _logger.LogInformation("File detected and processed: {FilePath}", filePath);
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Data Capture Service");
            
            if (_fileWatcher != null)
            {
                await _fileWatcher.StopAsync(cancellationToken);
                _fileWatcher.Dispose();
            }

            await base.StopAsync(cancellationToken);
        }
    }
}