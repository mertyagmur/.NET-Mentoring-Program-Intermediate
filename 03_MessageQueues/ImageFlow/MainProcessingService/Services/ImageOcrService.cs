using ImageFlow.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Tesseract;

namespace MainProcessingService.Services
{
    public class ImageOcrService : IOcrService, IDisposable
    {
        private readonly ILogger<ImageOcrService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _semaphore;
        private readonly string[] _supportedExtensions;
        private TesseractEngine? _ocrEngine;
        private bool _disposed = false;

        public ImageOcrService(ILogger<ImageOcrService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            var maxConcurrentOperations = configuration.GetValue<int>("Ocr:MaxConcurrentOperations", Environment.ProcessorCount);
            _semaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
            _supportedExtensions = new[] { "png", "jpg", "jpeg", "bmp", "tif", "tiff" };
            
            InitializeEngine();
        }

        private void InitializeEngine()
        {
            try
            {
                var tesseractPath = _configuration["Ocr:TesseractPath"] ?? @"C:\Program Files\Tesseract-OCR\tessdata";
                var language = _configuration["Ocr:Language"] ?? "eng";
                
                _ocrEngine = new TesseractEngine(tesseractPath, language, EngineMode.Default);
                _logger.LogInformation("OCR engine initialized successfully with language: {Language}", language);
            }
            catch (Exception ex)
            {
                var tesseractPath = _configuration["Ocr:TesseractPath"] ?? @"C:\Program Files\Tesseract-OCR\tessdata";
                var language = _configuration["Ocr:Language"] ?? "eng";
                _logger.LogError(ex, "Failed to initialize OCR engine. Path: {Path}, Language: {Language}", 
                    tesseractPath, language);
                throw;
            }
        }

        public async Task<string> PerformOcrAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ImageOcrService));

            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");

            if (!SupportsFileType(Path.GetExtension(imagePath)))
                throw new NotSupportedException($"File type not supported for OCR: {Path.GetExtension(imagePath)}");

            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                _logger.LogDebug("Starting OCR for image: {ImagePath}", imagePath);
                
                using var img = Pix.LoadFromFile(imagePath);
                using var page = _ocrEngine!.Process(img);
                
                var text = page.GetText().Trim();
                var confidence = page.GetMeanConfidence();
                
                _logger.LogInformation("OCR completed for {ImagePath}. Confidence: {Confidence:P2}, Text length: {Length}", 
                    imagePath, confidence, text.Length);
                
                if (confidence < 0.7)
                {
                    _logger.LogWarning("Low OCR confidence ({Confidence:P2}) for image: {ImagePath}", confidence, imagePath);
                }
                
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR failed for image: {ImagePath}", imagePath);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public bool SupportsFileType(string fileExtension)
        {
            var extension = fileExtension?.TrimStart('.').ToLowerInvariant();
            return !string.IsNullOrEmpty(extension) && _supportedExtensions.Contains(extension);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _ocrEngine?.Dispose();
                _semaphore?.Dispose();
                _disposed = true;
                _logger.LogInformation("OCR service disposed");
            }
        }
    }
}
