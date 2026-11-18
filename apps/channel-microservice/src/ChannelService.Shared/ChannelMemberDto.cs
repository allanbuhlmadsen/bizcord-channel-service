namespace ChannelService.Shared;

public sealed class ChannelMemberDto
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTimeOffset JoinedAt { get; set; }
}