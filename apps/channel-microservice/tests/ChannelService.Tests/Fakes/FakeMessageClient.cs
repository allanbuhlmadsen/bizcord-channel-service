using ChannelService.Messaging;

namespace ChannelService.Tests.Fakes;

public sealed class FakeMessageClient : IMessageClient
{
    private readonly List<object> _published = new();

    public IReadOnlyList<object> Published => _published;

    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        _published.Add(message);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TMessage>(
        string subscriptionId,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return Task.CompletedTask;
    }

    public TMessage SinglePublished<TMessage>()
        where TMessage : class
    {
        return _published.OfType<TMessage>().Single();
    }
}