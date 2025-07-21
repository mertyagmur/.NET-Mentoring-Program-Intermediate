namespace ImageFlow.Shared.Interfaces
{
    public interface IMessageQueue : IAsyncDisposable
    {
        Task SendAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
        Task StartConsumingAsync<T>(Func<T, Task> messageHandler, CancellationToken cancellationToken = default) where T : class;
    }
} 