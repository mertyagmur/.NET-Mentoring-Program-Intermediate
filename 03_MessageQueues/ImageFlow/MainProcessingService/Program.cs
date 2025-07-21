using MainProcessingService.Services;
using ImageFlow.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MainProcessingService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/processing-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting Main Processing Service");

                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Main Processing Service terminated unexpectedly");
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

                    services.AddSingleton<IOcrService>(serviceProvider =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<ImageOcrService>>();
                        var config = serviceProvider.GetRequiredService<IConfiguration>();
                        return new ImageOcrService(logger, config);
                    });

                    services.AddSingleton<IFileProcessor, ImageFileProcessor>();

                    services.AddSingleton<FileAssembler>();

                    services.AddHostedService<MessageConsumerWorker>();
                });
    }

    public class MessageConsumerWorker : BackgroundService
    {
        private readonly ILogger<MessageConsumerWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly FileAssembler _fileAssembler;
        private ChunkReceiverService? _receiver;

        public MessageConsumerWorker(
            ILogger<MessageConsumerWorker> logger,
            IConfiguration configuration,
            FileAssembler fileAssembler)
        {
            _logger = logger;
            _configuration = configuration;
            _fileAssembler = fileAssembler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var queueName = _configuration["RabbitMq:QueueName"] ?? "imageflow_queue";
                var host = _configuration["RabbitMq:Host"] ?? "localhost";
                
                _logger.LogInformation("Starting message consumer for queue: {Queue}", queueName);

                _receiver = await ChunkReceiverService.CreateAsync(host, queueName, _fileAssembler, stoppingToken);

                await _receiver.StartAsync();

                _logger.LogInformation("Main Processing Service is running and listening for messages");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Message consumer is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Message Consumer Worker");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Main Processing Service");

            if (_receiver != null)
            {
                await _receiver.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
        }
    }
}