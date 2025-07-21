namespace ImageFlow.Shared.Interfaces
{
    public interface IFileProcessor
    {
        Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
        bool CanProcess(string fileExtension);
        int Priority { get; }
    }
} 