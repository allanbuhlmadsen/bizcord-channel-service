using ChannelService.Data;
using ChannelService.Models;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace ChannelService.Messaging;

// Channel Service's step in the user ban saga.
public sealed class RemoveUserFromChannelsHandler
    : IMessageHandler<RemoveUserFromChannels>
{
    private readonly IChannelRepository _channels;
    private readonly IRemovedMembershipRepository _removals;
    private readonly IMessageClient _messageClient;
    private readonly ILogger<RemoveUserFromChannelsHandler> _logger;

    public RemoveUserFromChannelsHandler(
        IChannelRepository channels,
        IRemovedMembershipRepository removals,
        IMessageClient messageClient,
        ILogger<RemoveUserFromChannelsHandler> logger)
    {
        _channels = channels;
        _removals = removals;
        _messageClient = messageClient;
        _logger = logger;
    }

    public async Task HandleAsync(
        RemoveUserFromChannels message,
        CancellationToken cancellationToken)
    {
        try
        {
            var channels = await _channels.GetAllAsync(cancellationToken);

            var affected = channels
                .Where(c => c.Members.Any(m => m.UserId == message.UserId))
                .ToList();

            var removals = affected
                .Select(channel => new RemovedMembership
                {
                    SagaId = message.SagaId,
                    ChannelId = channel.Id,
                    UserId = message.UserId,
                    Role = channel.Members.First(m => m.UserId == message.UserId).Role
                })
                .ToList();

            // Written down before anything is removed. If the loop below fails
            // halfway, the memberships already gone can still be restored.
            await _removals.AddRangeAsync(removals, cancellationToken);

            foreach (var channel in affected)
            {
                channel.RemoveMember(message.UserId);
                await _channels.UpdateAsync(channel, cancellationToken);
            }

            _logger.LogInformation(
                "Saga {SagaId}: removed user {UserId} from {Count} channels",
                message.SagaId,
                message.UserId,
                removals.Count);

            await _messageClient.PublishAsync(
                new UserRemovedFromChannels
                {
                    SagaId = message.SagaId,
                    UserId = message.UserId,
                    ChannelCount = removals.Count
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Saga {SagaId}: removing user {UserId} from channels failed",
                message.SagaId,
                message.UserId);

            await _messageClient.PublishAsync(
                new ChannelRemovalFailed
                {
                    SagaId = message.SagaId,
                    UserId = message.UserId,
                    Reason = exception.Message
                },
                cancellationToken);
        }
    }
}