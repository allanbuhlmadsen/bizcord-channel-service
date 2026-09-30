using ChannelService.Messaging;

namespace ChannelService.Tests.Infrastructure;

public sealed class MessageCapture<TMessage>
    where TMessage : class
{
    private readonly TaskCompletionSource<TMessage> _received =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private MessageCapture()
    {
    }

    public static async Task<MessageCapture<TMessage>> StartAsync(
        IMessageClient messageClient,
        CancellationToken cancellationToken = default)
    {
        var capture = new MessageCapture<TMessage>();

        await messageClient.SubscribeAsync<TMessage>(
            $"test-capture-{Guid.NewGuid():N}",
            (message, _) =>
            {
                capture._received.TrySetResult(message);
                return Task.CompletedTask;
            },
            cancellationToken);

        return capture;
    }

    public async Task<TMessage> WaitForMessageAsync(TimeSpan timeout)
    {
        var completed = await Task.WhenAny(
            _received.Task,
            Task.Delay(timeout));

        if (completed != _received.Task)
        {
            throw new TimeoutException(
                $"No {typeof(TMessage).Name} received within {timeout}.");
        }

        return await _received.Task;
    }
}