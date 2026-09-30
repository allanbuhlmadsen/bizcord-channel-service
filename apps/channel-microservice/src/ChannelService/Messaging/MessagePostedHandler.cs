using ChannelService.Data;
using Shared.Contracts.Events;

namespace ChannelService.Messaging;

public sealed class MessagePostedHandler : IMessageHandler<MessagePostedEvent>
{
    private readonly IChannelRepository _repository;
    private readonly IMessageClient _messageClient;
    private readonly ILogger<MessagePostedHandler> _logger;

    public MessagePostedHandler(
        IChannelRepository repository,
        IMessageClient messageClient,
        ILogger<MessagePostedHandler> logger)
    {
        _repository = repository;
        _messageClient = messageClient;
        _logger = logger;
    }

    public async Task HandleAsync(
        MessagePostedEvent message,
        CancellationToken cancellationToken)
    {
        var channel = await _repository.GetByIdAsync(message.ChannelId, cancellationToken);

        if (channel is null)
        {
            _logger.LogWarning(
                "Ignoring MessagePostedEvent for unknown channel {ChannelId}",
                message.ChannelId);
            return;
        }

        var occurredAt = new DateTimeOffset(message.PostedAt, TimeSpan.Zero);

        channel.RecordActivity(occurredAt);
        await _repository.UpdateAsync(channel, cancellationToken);

        await _messageClient.PublishAsync(
            new ChannelActivityUpdatedEvent
            {
                MessageId = message.MessageId,
                ChannelId = channel.Id,
                LastActivityAt = channel.LastActivityAt!.Value.UtcDateTime,
                ProcessedAt = DateTime.UtcNow
            },
            cancellationToken);
    }
}