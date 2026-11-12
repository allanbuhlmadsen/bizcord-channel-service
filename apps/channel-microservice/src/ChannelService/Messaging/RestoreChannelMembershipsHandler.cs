using ChannelService.Data;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace ChannelService.Messaging;

// The compensating action for Channel Service's step in the user ban saga.
public sealed class RestoreChannelMembershipsHandler
    : IMessageHandler<RestoreChannelMemberships>
{
    private readonly IChannelRepository _channels;
    private readonly IRemovedMembershipRepository _removals;
    private readonly IMessageClient _messageClient;
    private readonly ILogger<RestoreChannelMembershipsHandler> _logger;

    public RestoreChannelMembershipsHandler(
        IChannelRepository channels,
        IRemovedMembershipRepository removals,
        IMessageClient messageClient,
        ILogger<RestoreChannelMembershipsHandler> logger)
    {
        _channels = channels;
        _removals = removals;
        _messageClient = messageClient;
        _logger = logger;
    }

    public async Task HandleAsync(
        RestoreChannelMemberships message,
        CancellationToken cancellationToken)
    {
        var removals = await _removals.GetBySagaAsync(message.SagaId, cancellationToken);
        var restored = 0;

        foreach (var removal in removals)
        {
            var channel = await _channels.GetByIdAsync(removal.ChannelId, cancellationToken);

            if (channel is null)
            {
                // The channel was deleted while the saga was running. There is
                // nothing to restore the membership to.
                _logger.LogWarning(
                    "Saga {SagaId}: channel {ChannelId} no longer exists, "
                    + "the membership cannot be restored",
                    message.SagaId,
                    removal.ChannelId);

                continue;
            }

            if (channel.Members.Any(m => m.UserId == removal.UserId))
            {
                // Either the removal never happened, or someone added the user
                // back. AddMember would throw, so leave it alone.
                continue;
            }

            channel.AddMember(removal.UserId, removal.Role);
            await _channels.UpdateAsync(channel, cancellationToken);
            restored++;
        }

        await _removals.DeleteBySagaAsync(message.SagaId, cancellationToken);

        _logger.LogInformation(
            "Saga {SagaId}: restored {Restored} of {Recorded} memberships for user {UserId}",
            message.SagaId,
            restored,
            removals.Count,
            message.UserId);

        await _messageClient.PublishAsync(
            new ChannelMembershipsRestored
            {
                SagaId = message.SagaId,
                UserId = message.UserId,
                ChannelCount = restored
            },
            cancellationToken);
    }
}