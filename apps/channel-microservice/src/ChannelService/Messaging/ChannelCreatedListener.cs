namespace ChannelService.Messaging;

// Temporary: subscribes to a message this same service publishes, purely
// to verify the message client. See the /channels endpoint in Program.cs.
public sealed class ChannelCreatedListener : BackgroundService
{
    private readonly IMessageClient _messageClient;
    private readonly ILogger<ChannelCreatedListener> _logger;

    public ChannelCreatedListener(
        IMessageClient messageClient,
        ILogger<ChannelCreatedListener> logger)
    {
        _messageClient = messageClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _messageClient.SubscribeAsync<ChannelCreated>(
            "channel-service",
            (message, _) =>
            {
                _logger.LogInformation(
                    "Received ChannelCreated: {Name} ({ChannelId})",
                    message.Name,
                    message.ChannelId);
                return Task.CompletedTask;
            },
            stoppingToken);
    }
}