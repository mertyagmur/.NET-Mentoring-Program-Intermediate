using ImageFlow.Shared.Models;
using ImageFlow.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MainProcessingService.Services
{
    public class FileAssembler
    {
        private readonly string _outputFolder;
        private readonly IEnumerable<IFileProcessor> _fileProcessors;
        private readonly ILogger<FileAssembler> _logger;

        private readonly ConcurrentDictionary<string, ConcurrentBag<FileChunkMessage>> _chunkBuffer = new();

        public FileAssembler(
            IEnumerable<IFileProcessor> fileProcessors, 
            ILogger<FileAssembler> logger,
            IConfiguration configuration)
        {
            _fileProcessors = fileProcessors.OrderBy(p => p.Priority);
            _logger = logger;
            _outputFolder = configuration["Processing:OutputFolder"] ?? "ProcessedImages";
            
            Directory.CreateDirectory(_outputFolder);
        }

        public async Task AddChunkAsync(FileChunkMessage chunk, CancellationToken cancellationToken = default)
        {
            if (chunk == null)
            {
                _logger.LogWarning("Received null chunk message");
                return;
            }

            var chunks = _chunkBuffer.GetOrAdd(chunk.FileId, _ => new ConcurrentBag<FileChunkMessage>());
            chunks.Add(chunk);

            _logger.LogDebug("Received chunk {ChunkNumber}/{TotalChunks} for {FileName}", 
                chunk.ChunkNumber, chunk.TotalChunks, chunk.FileName);

            if (chunks.Count == chunk.TotalChunks)
            {
                await ReassembleAndProcessFileAsync(chunk.FileId, cancellationToken);
            }
        }

        private async Task ReassembleAndProcessFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            if (!_chunkBuffer.TryRemove(fileId, out var chunks))
            {
                _logger.LogWarning("Failed to retrieve chunks for file ID: {FileId}", fileId);
                return;
            }

            try
            {
                var orderedChunks = chunks.OrderBy(c => c.ChunkNumber).ToList();
                var fileName = orderedChunks[0].FileName;
                var sanitizedFileName = SanitizeFileName(fileName);
                var outputPath = Path.Combine(_outputFolder, sanitizedFileName);

                outputPath = GetUniqueFilePath(outputPath);

                await ReassembleFileAsync(orderedChunks, outputPath, cancellationToken);
                
                _logger.LogInformation("Reassembled and saved: {OutputPath}", outputPath);

                await ProcessFileAsync(outputPath, orderedChunks[0].FileType, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reassemble and process file: {FileId}", fileId);
                throw;
            }
        }

        private async Task ReassembleFileAsync(List<FileChunkMessage> chunks, string outputPath, CancellationToken cancellationToken)
        {
            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            
            foreach (var chunk in chunks)
            {
                await fs.WriteAsync(chunk.ChunkData, cancellationToken);
            }
            
            await fs.FlushAsync(cancellationToken);
        }

        private async Task ProcessFileAsync(string filePath, string fileType, CancellationToken cancellationToken)
        {
            var processor = _fileProcessors.FirstOrDefault(p => p.CanProcess(fileType));
            
            if (processor != null)
            {
                _logger.LogInformation("Processing file {FilePath} with {ProcessorType}", filePath, processor.GetType().Name);
                await processor.ProcessFileAsync(filePath, cancellationToken);
            }
            else
            {
                _logger.LogDebug("No processor found for file type: {FileType}", fileType);
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(sanitized) ? "unnamed_file" : sanitized;
        }

        private static string GetUniqueFilePath(string basePath)
        {
            if (!File.Exists(basePath))
                return basePath;

            var directory = Path.GetDirectoryName(basePath)!;
            var fileName = Path.GetFileNameWithoutExtension(basePath);
            var extension = Path.GetExtension(basePath);
            var counter = 1;

            string uniquePath;
            do
            {
                uniquePath = Path.Combine(directory, $"{fileName}_{counter}{extension}");
                counter++;
            } while (File.Exists(uniquePath));

            return uniquePath;
        }
    }
}
