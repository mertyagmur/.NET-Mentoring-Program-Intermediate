using ImageFlow.Shared.Interfaces;
using ImageFlow.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Security.Cryptography;
using System.Text.Json;

namespace DataCaptureService.Services
{
    public class FileChunkSender : IFileChunkSender
    {
        private readonly ILogger<FileChunkSender> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _chunkSizeBytes;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private bool _disposed = false;

        private FileChunkSender(
            ILogger<FileChunkSender> logger,
            IConfiguration configuration,
            IConnection connection, 
            IChannel channel)
        {
            _logger = logger;
            _configuration = configuration;
            var chunkSizeKb = configuration.GetValue<int>("Processing:ChunkSizeKb", 1024);
            _chunkSizeBytes = chunkSizeKb * 1024;
            _connection = connection;
            _channel = channel;
        }

        public static async Task<FileChunkSender> CreateAsync(
            ILogger<FileChunkSender> logger,
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
                ClientProvidedName = "FileChunkSender"
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

                logger.LogInformation("FileChunkSender connected to RabbitMQ at {Host}:{Port}", host, port);

                return new FileChunkSender(logger, configuration, connection, channel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create FileChunkSender connection to RabbitMQ");
                throw;
            }
        }

        public async Task SendFileInChunksAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileChunkSender));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);
            var sanitizedFileName = SanitizeFileName(fileName);
            var fileId = Guid.NewGuid().ToString();
            var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / _chunkSizeBytes);

            _logger.LogInformation("Starting to send file {FileName} ({FileSize} bytes) in {TotalChunks} chunks", 
                sanitizedFileName, fileInfo.Length, totalChunks);

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                using var md5 = MD5.Create();
                
                var buffer = new byte[_chunkSizeBytes];
                var chunkNumber = 1;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var bytesRead = await fileStream.ReadAsync(buffer, 0, _chunkSizeBytes, cancellationToken);
                    if (bytesRead == 0)
                        break;

                    var chunkData = new byte[bytesRead];
                    Array.Copy(buffer, 0, chunkData, 0, bytesRead);

                    var checksum = Convert.ToBase64String(md5.ComputeHash(chunkData));

                    var message = new FileChunkMessage
                    {
                        FileId = fileId,
                        FileName = sanitizedFileName,
                        ChunkNumber = chunkNumber,
                        TotalChunks = totalChunks,
                        FileType = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant(),
                        ChunkData = chunkData,
                        OriginalFileSize = fileInfo.Length,
                        ChecksumMd5 = checksum
                    };

                    await SendChunkAsync(message, cancellationToken);

                    _logger.LogDebug("Sent chunk {ChunkNumber}/{TotalChunks} for {FileName}", 
                        chunkNumber, totalChunks, sanitizedFileName);

                    chunkNumber++;
                }

                _logger.LogInformation("Successfully sent {FileName} in {TotalChunks} chunks", 
                    sanitizedFileName, totalChunks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send file {FileName} in chunks", sanitizedFileName);
                throw;
            }
        }

        private async Task SendChunkAsync(FileChunkMessage message, CancellationToken cancellationToken)
        {
            var queueName = _configuration["RabbitMq:QueueName"] ?? "imageflow_queue";
            var body = JsonSerializer.SerializeToUtf8Bytes(message);

            var props = new BasicProperties
            {
                Persistent = true,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                ContentType = "application/json",
                Headers = new Dictionary<string, object?>
                {
                    ["file-id"] = message.FileId,
                    ["chunk-number"] = message.ChunkNumber,
                    ["total-chunks"] = message.TotalChunks,
                    ["file-name"] = message.FileName
                }
            };

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(sanitized) ? "unnamed_file" : sanitized;
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
                _logger.LogInformation("FileChunkSender disposed");
            }
        }
    }
}
