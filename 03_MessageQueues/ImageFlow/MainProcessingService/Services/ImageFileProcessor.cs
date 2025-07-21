using ImageFlow.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace MainProcessingService.Services
{
    public class ImageFileProcessor : IFileProcessor
    {
        private readonly IOcrService _ocrService;
        private readonly ILogger<ImageFileProcessor> _logger;
        private readonly string[] _supportedExtensions = { "png", "jpg", "jpeg", "bmp", "tif", "tiff" };

        public int Priority => 1;

        public ImageFileProcessor(IOcrService ocrService, ILogger<ImageFileProcessor> logger)
        {
            _ocrService = ocrService;
            _logger = logger;
        }

        public bool CanProcess(string fileExtension)
        {
            var extension = fileExtension?.TrimStart('.').ToLowerInvariant();
            return !string.IsNullOrEmpty(extension) && _supportedExtensions.Contains(extension);
        }

        public async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found for processing: {FilePath}", filePath);
                return;
            }

            try
            {
                _logger.LogInformation("Starting OCR processing for image: {FilePath}", filePath);
                
                var text = await _ocrService.PerformOcrAsync(filePath, cancellationToken);
                var textFilePath = Path.ChangeExtension(filePath, ".txt");
                
                await File.WriteAllTextAsync(textFilePath, text, cancellationToken);
                
                _logger.LogInformation("OCR text saved: {TextFilePath}", textFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process image file: {FilePath}", filePath);
                throw;
            }
        }
    }
} 