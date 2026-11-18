using ChannelService.Service;
using ChannelService.Shared;

namespace ChannelService.GraphQL;

// The read side of the GraphQL layer. Every resolver goes through
// IChannelAppService — the same service layer the REST controllers use, so the
// two presentation layers cannot drift apart.
public class ChannelQuery
{
    public Task<IEnumerable<ChannelDto>> GetChannels(
        IChannelAppService service,
        CancellationToken cancellationToken)
    {
        return service.GetAllAsync(cancellationToken);
    }

    public Task<ChannelDto?> GetChannel(
        Guid id,
        IChannelAppService service,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }

    public Task<IEnumerable<ChannelDto>> GetChannelsForUser(
        Guid userId,
        IChannelAppService service,
        CancellationToken cancellationToken)
    {
        return service.GetForUserAsync(userId, cancellationToken);
    }
}