namespace ImageFlow.Shared.Interfaces
{
    public interface IFileWatcher : IDisposable
    {
        event Func<string, Task> FileDetected;
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
    }
} 