using Microsoft.Extensions.Logging;

namespace ImageFlow.Shared.Services
{
    public class RetryPolicy
    {
        private readonly ILogger<RetryPolicy> _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _baseDelay;

        public RetryPolicy(ILogger<RetryPolicy> logger, int maxRetries = 3, TimeSpan? baseDelay = null)
        {
            _logger = logger;
            _maxRetries = maxRetries;
            _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        }

        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            Predicate<Exception>? shouldRetry = null,
            CancellationToken cancellationToken = default)
        {
            shouldRetry ??= _ => true;
            var attempt = 0;

            while (true)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (attempt < _maxRetries && shouldRetry(ex))
                {
                    attempt++;
                    var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    
                    _logger.LogWarning(ex, "Operation failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms", 
                        attempt, _maxRetries, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        public async Task ExecuteAsync(
            Func<Task> operation,
            Predicate<Exception>? shouldRetry = null,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            }, shouldRetry, cancellationToken);
        }
    }
} 