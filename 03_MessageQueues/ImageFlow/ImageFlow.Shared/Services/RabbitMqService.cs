using ImageFlow.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace ImageFlow.Shared.Services
{
    public class RabbitMqService : IMessageQueue
    {
        private readonly ILogger<RabbitMqService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private bool _disposed = false;

        private RabbitMqService(ILogger<RabbitMqService> logger, IConfiguration configuration, IConnection connection, IChannel channel)
        {
            _logger = logger;
            _configuration = configuration;
            _connection = connection;
            _channel = channel;
        }

        public static async Task<RabbitMqService> CreateAsync(
            ILogger<RabbitMqService> logger, 
            IConfiguration configuration, 
            CancellationToken cancellationToken = default)
        {
            var host = configuration["RabbitMq:Host"] ?? "localhost";
            var port = configuration.GetValue<int>("RabbitMq:Port", 5672);
            var username = configuration["RabbitMq:Username"] ?? "guest";
            var password = configuration["RabbitMq:Password"] ?? "guest";
            var queueName = configuration["RabbitMq:QueueName"] ?? "imageflow_queue";

            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = username,
                Password = password,
                ClientProvidedName = "ImageFlow-Service"
            };

            try
            {
                var connection = await factory.CreateConnectionAsync(cancellationToken);
                var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}, Queue: {Queue}", 
                    host, port, queueName);

                return new RabbitMqService(logger, configuration, connection, channel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port}", host, port);
                throw;
            }
        }

        public async Task SendAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMqService));

            var queueName = _configuration["RabbitMq:QueueName"] ?? "imageflow_queue";

            try
            {
                var body = JsonSerializer.SerializeToUtf8Bytes(message);
                var props = new BasicProperties
                {
                    Persistent = true,
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    ContentType = "application/json"
                };

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    mandatory: false,
                    basicProperties: props,
                    body: body,
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Message sent to queue {Queue}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to queue {Queue}", queueName);
                throw;
            }
        }

        public async Task StartConsumingAsync<T>(Func<T, Task> messageHandler, CancellationToken cancellationToken = default) where T : class
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMqService));

            var queueName = _configuration["RabbitMq:QueueName"] ?? "imageflow_queue";

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<T>(ea.Body.Span);
                    if (message != null)
                    {
                        await messageHandler(message);
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize message from queue {Queue}", queueName);
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {Queue}", queueName);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Started consuming messages from queue {Queue}", queueName);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (_channel != null)
                {
                    await _channel.CloseAsync();
                    _channel.Dispose();
                }

                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    _connection.Dispose();
                }

                _disposed = true;
                _logger.LogInformation("RabbitMQ service disposed");
            }
        }
    }
} 