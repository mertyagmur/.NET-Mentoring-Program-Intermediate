namespace ImageFlow.Shared.Interfaces
{
    public interface IFileChunkSender : IAsyncDisposable
    {
        Task SendFileInChunksAsync(string filePath, CancellationToken cancellationToken = default);
    }
} 