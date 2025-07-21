namespace ImageFlow.Shared.Interfaces
{
    public interface IOcrService
    {
        Task<string> PerformOcrAsync(string imagePath, CancellationToken cancellationToken = default);
        bool SupportsFileType(string fileExtension);
    }
} 