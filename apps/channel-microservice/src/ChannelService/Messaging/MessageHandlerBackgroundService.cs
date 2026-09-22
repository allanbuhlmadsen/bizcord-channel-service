using System.Reflection;

namespace ChannelService.Messaging;

public sealed class MessageHandlerBackgroundService : BackgroundService
{
    private const string ServiceName = "channel-service";

    private static readonly MethodInfo SubscribeHandlersMethod =
        typeof(MessageHandlerBackgroundService).GetMethod(
            nameof(SubscribeHandlersAsync),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly IMessageClient _messageClient;
    private readonly MessageHandlerRegistry _registry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageHandlerBackgroundService> _logger;

    public MessageHandlerBackgroundService(
        IMessageClient messageClient,
        MessageHandlerRegistry registry,
        IServiceScopeFactory scopeFactory,
        ILogger<MessageHandlerBackgroundService> logger)
    {
        _messageClient = messageClient;
        _registry = registry;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var messageType in _registry.MessageTypes)
        {
            var subscribe = SubscribeHandlersMethod.MakeGenericMethod(messageType);
            await (Task)subscribe.Invoke(this, new object[] { stoppingToken })!;
        }
    }

    private async Task SubscribeHandlersAsync<TMessage>(CancellationToken stoppingToken)
        where TMessage : class
    {
        var subscriptionId = $"{ServiceName}.{typeof(TMessage).Name}";

        await _messageClient.SubscribeAsync<TMessage>(
            subscriptionId,
            DispatchAsync,
            stoppingToken);

        _logger.LogInformation("Subscribed to {SubscriptionId}", subscriptionId);
    }

    private async Task DispatchAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        using var scope = _scopeFactory.CreateScope();

        var handlers = scope.ServiceProvider
            .GetServices<IMessageHandler<TMessage>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(message, cancellationToken);
        }
    }
}