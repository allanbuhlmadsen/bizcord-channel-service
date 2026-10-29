using EasyNetQ;
using Polly;
using Polly.Retry;

namespace ChannelService.Messaging;

public sealed class EasyNetQMessageClient : IMessageClient
{
    private readonly IBus _bus;
    private readonly ResiliencePipeline _publishPipeline;

    public EasyNetQMessageClient(IBus bus, ILogger<EasyNetQMessageClient> logger)
    {
        _bus = bus;

        _publishPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = arguments =>
                {
                    logger.LogWarning(
                        "Publish failed, retrying in {Delay}. Attempt {Attempt} of 3.",
                        arguments.RetryDelay,
                        arguments.AttemptNumber + 2);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(1))
            .Build();
    }

    public async Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        await _publishPipeline.ExecuteAsync(
            async token => await _bus.PubSub.PublishAsync(message, token),
            cancellationToken);
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