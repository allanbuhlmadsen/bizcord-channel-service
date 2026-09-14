using ChannelService.Shared;

namespace ChannelService.Models;

public sealed class ChannelConverter : IConverter<Channel, ChannelDto>
{
    public ChannelDto Convert(Channel entity)
    {
        return new ChannelDto
        {
            Id = entity.Id,
            Name = entity.Name.Value,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            MemberCount = entity.Members.Count
        };
    }
}