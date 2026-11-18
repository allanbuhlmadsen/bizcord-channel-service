using ChannelService.Contracts;
using ChannelService.Service;
using ChannelService.Shared;

namespace ChannelService.GraphQL;

// The write side. The input type belongs to this presentation layer, not to the
// REST contract, so the two can be shaped for their own callers.
public class ChannelMutation
{
    public Task<ChannelDto> CreateChannel(
        CreateChannelInput input,
        IChannelAppService service,
        CancellationToken cancellationToken)
    {
        var request = new CreateChannelRequest
        {
            Name = input.Name,
            Description = input.Description
        };

        return service.CreateAsync(request, input.CreatedBy, cancellationToken);
    }
}

public sealed record CreateChannelInput(
    string Name,
    string? Description,
    Guid? CreatedBy);