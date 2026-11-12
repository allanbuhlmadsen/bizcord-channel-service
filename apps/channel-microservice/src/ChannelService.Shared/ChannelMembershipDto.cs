namespace ChannelService.Shared;

public sealed class ChannelMembershipDto
{
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }
    public bool IsMember { get; set; }
    public string? Role { get; set; }
}