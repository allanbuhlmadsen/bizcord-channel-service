using ChannelService.Service;
using ChannelService.Shared;
using HotChocolate;
using HotChocolate.Types;

namespace ChannelService.GraphQL;

// Adds a members field to the channel type. This is where GraphQL earns its
// place: the REST API has no endpoint that returns a channel together with its
// members, so a client needing both has to make several calls. Here it asks for
// both in one query — and only pays for the members when it asks for them.
[ExtendObjectType(typeof(ChannelDto))]
public class ChannelDtoExtensions
{
    public Task<IEnumerable<ChannelMemberDto>?> GetMembers(
        [Parent] ChannelDto channel,
        IChannelAppService service,
        CancellationToken cancellationToken)
    {
        return service.GetMembersAsync(channel.Id, cancellationToken);
    }
}