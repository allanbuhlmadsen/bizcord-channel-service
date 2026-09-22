namespace ChannelService.Messaging;

// Temporary: handles a message this same service publishes, purely to
// verify that handler discovery works. In the real system another
// service would handle ChannelCreated.
public sealed class ChannelCreatedHandler : IMessageHandler<ChannelCreated>
{
    private readonly ILogger<ChannelCreatedHandler> _logger;

    public ChannelCreatedHandler(ILogger<ChannelCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ChannelCreated message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received ChannelCreated: {Name} ({ChannelId})",
            message.Name,
            message.ChannelId);

        return Task.CompletedTask;
    }
}