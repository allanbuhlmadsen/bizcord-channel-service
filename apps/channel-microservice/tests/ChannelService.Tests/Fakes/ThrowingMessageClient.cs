using ChannelService.Messaging;

namespace ChannelService.Tests.Fakes;

// A message client that cannot reach the broker. Stands in for an outage
// so the service's behaviour under one can be asserted.
public sealed class ThrowingMessageClient : IMessageClient
{
    public int PublishAttempts { get; private set; }

    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        PublishAttempts++;
        throw new InvalidOperationException("Broker unavailable.");
    }

    public Task SubscribeAsync<TMessage>(
        string subscriptionId,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return Task.CompletedTask;
    }
}