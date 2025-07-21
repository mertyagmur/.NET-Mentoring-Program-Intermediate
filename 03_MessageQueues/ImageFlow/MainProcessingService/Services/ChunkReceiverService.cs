using ImageFlow.Shared.Models;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace MainProcessingService.Services
{
    public class ChunkReceiverService : IAsyncDisposable
    {
        private readonly string _queueName;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly FileAssembler _assembler;
        private readonly ILogger<ChunkReceiverService> _logger;

        private ChunkReceiverService(
            string queueName, 
            IConnection connection, 
            IChannel channel, 
            FileAssembler assembler,
            ILogger<ChunkReceiverService> logger)
        {
            _queueName = queueName;
            _connection = connection;
            _channel = channel;
            _assembler = assembler;
            _logger = logger;
        }

        public static async Task<ChunkReceiverService> CreateAsync(
            string host, 
            string queueName, 
            FileAssembler assembler, 
            CancellationToken cancellationToken = default)
        {
            var factory = new ConnectionFactory
            {
                HostName = host
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

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

                var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ChunkReceiverService>();
                logger.LogInformation("Connected to RabbitMQ for chunk receiving. Queue: {Queue}", queueName);

                return new ChunkReceiverService(queueName, connection, channel, assembler, logger);
            }
            catch (Exception ex)
            {
                var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ChunkReceiverService>();
                logger.LogError(ex, "Failed to create ChunkReceiverService connection to RabbitMQ");
                throw;
            }
        }

        public async Task StartAsync()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var chunk = JsonSerializer.Deserialize<FileChunkMessage>(ea.Body.Span);
                    if (chunk == null)
                    {
                        _logger.LogWarning("Failed to deserialize chunk message");
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }

                    if (!ValidateChunk(chunk))
                    {
                        _logger.LogWarning("Invalid chunk message received: {FileId}, Chunk: {ChunkNumber}", 
                            chunk.FileId, chunk.ChunkNumber);
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }

                    if (!IsSecureFileName(chunk.FileName) || !IsSupportedFileType(chunk.FileType))
                    {
                        _logger.LogWarning("Security validation failed for chunk: {FileName}, Type: {FileType}", 
                            chunk.FileName, chunk.FileType);
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }

                    await _assembler.AddChunkAsync(chunk);
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);

                    _logger.LogDebug("Successfully processed chunk {ChunkNumber}/{TotalChunks} for file {FileName}", 
                        chunk.ChunkNumber, chunk.TotalChunks, chunk.FileName);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON deserialization error for message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing chunk message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Started consuming messages from queue: {Queue}", _queueName);
        }

        private bool ValidateChunk(FileChunkMessage chunk)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(chunk);
            
            var isValid = Validator.TryValidateObject(chunk, validationContext, validationResults, true);
            
            if (!isValid)
            {
                _logger.LogWarning("Chunk validation failed: {Errors}", 
                    string.Join(", ", validationResults.Select(r => r.ErrorMessage)));
            }

            if (chunk.ChunkNumber <= 0 || chunk.ChunkNumber > chunk.TotalChunks)
            {
                _logger.LogWarning("Invalid chunk number: {ChunkNumber} of {TotalChunks}", 
                    chunk.ChunkNumber, chunk.TotalChunks);
                return false;
            }

            if (chunk.ChunkData.Length == 0)
            {
                _logger.LogWarning("Empty chunk data for chunk {ChunkNumber}", chunk.ChunkNumber);
                return false;
            }

            return isValid;
        }

        private bool IsSecureFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            if (fileName.Contains("..") || fileName.Contains("\\") || fileName.Contains("/"))
                return false;

            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", 
                "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", 
                "LPT6", "LPT7", "LPT8", "LPT9" };
            
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
            return !reservedNames.Contains(nameWithoutExtension);
        }

        private bool IsSupportedFileType(string fileType)
        {
            var supportedTypes = new[] { "png", "jpg", "jpeg", "bmp", "tif", "tiff", "pdf", "txt" };
            return !string.IsNullOrWhiteSpace(fileType) && 
                   supportedTypes.Contains(fileType.ToLowerInvariant());
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
            }

            if (_connection is not null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }

            _logger.LogInformation("ChunkReceiverService disposed");
        }
    }
}
