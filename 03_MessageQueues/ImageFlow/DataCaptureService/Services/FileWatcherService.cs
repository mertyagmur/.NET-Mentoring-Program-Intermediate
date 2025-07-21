using ImageFlow.Shared.Interfaces;
using ImageFlow.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataCaptureService.Services
{
    public class FileWatcherService : IFileWatcher
    {
        private readonly string _folderPath;
        private readonly IFileChunkSender _sender;
        private readonly ILogger<FileWatcherService> _logger;
        private readonly IConfiguration _configuration;
        private readonly RetryPolicy _retryPolicy;
        private FileSystemWatcher? _watcher;
        private bool _disposed = false;

        public event Func<string, Task> FileDetected = _ => Task.CompletedTask;

        public FileWatcherService(
            string folderPath, 
            IFileChunkSender sender, 
            ILogger<FileWatcherService> logger,
            IConfiguration configuration,
            RetryPolicy retryPolicy)
        {
            _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileWatcherService));

            try
            {
                Directory.CreateDirectory(_folderPath);

                var internalBufferSizeKb = _configuration.GetValue<int>("FileWatcher:InternalBufferSizeKb", 64);

                _watcher = new FileSystemWatcher(_folderPath)
                {
                    EnableRaisingEvents = true,
                    Filter = "*.*",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size,
                    InternalBufferSize = internalBufferSizeKb * 1024
                };

                _watcher.Created += async (s, e) => await OnFileCreatedAsync(e.FullPath);
                _watcher.Error += OnWatcherError;

                _logger.LogInformation("File watcher started for folder: {FolderPath}", _folderPath);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start file watcher for folder: {FolderPath}", _folderPath);
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
                _logger.LogInformation("File watcher stopped for folder: {FolderPath}", _folderPath);
            }
            return Task.CompletedTask;
        }

        private async Task OnFileCreatedAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("File detected: {FilePath}", filePath);

                var isReady = await WaitUntilFileIsReadyAsync(filePath);
                if (!isReady)
                {
                    _logger.LogWarning("File {FilePath} was not ready within timeout period", filePath);
                    return;
                }

                await _retryPolicy.ExecuteAsync(
                    () => _sender.SendFileInChunksAsync(filePath),
                    ex => ex is not FileNotFoundException); 

                await FileDetected(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
            }
        }

        private async Task<bool> WaitUntilFileIsReadyAsync(string path)
        {
            var timeoutMs = _configuration.GetValue<int>("FileWatcher:FileReadyTimeoutMs", 30000);
            var checkIntervalMs = _configuration.GetValue<int>("FileWatcher:FileReadyCheckIntervalMs", 500);
            
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);
            var checkInterval = TimeSpan.FromMilliseconds(checkIntervalMs);
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < timeout)
            {
                try
                {
                    using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                    _logger.LogDebug("File is ready: {FilePath}", path);
                    return true;
                }
                catch (FileNotFoundException)
                {
                    _logger.LogWarning("File was deleted before processing: {FilePath}", path);
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    await Task.Delay(checkInterval);
                }
                catch (IOException)
                {
                    await Task.Delay(checkInterval);
                }
            }

            _logger.LogWarning("File readiness check timed out after {Timeout}ms: {FilePath}", timeoutMs, path);
            return false;
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            _logger.LogError(e.GetException(), "File watcher error occurred");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopAsync().Wait(TimeSpan.FromSeconds(5));
                _disposed = true;
            }
        }
    }
}
