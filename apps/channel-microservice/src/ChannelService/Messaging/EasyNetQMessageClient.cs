using EasyNetQ;

namespace ChannelService.Messaging;

public sealed class EasyNetQMessageClient : IMessageClient
{
    private readonly IBus _bus;

    public EasyNetQMessageClient(IBus bus)
    {
        _bus = bus;
    }

    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return _bus.PubSub.PublishAsync(message, cancellationToken);
    }

    public async Task SubscribeAsync<TMessage>(
        string subscriptionId,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        await _bus.PubSub.SubscribeAsync(
            subscriptionId,
            handler,
            configure => { },
            cancellationToken);
    }
}