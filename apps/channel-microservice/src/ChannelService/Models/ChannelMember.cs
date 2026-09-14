namespace ChannelService.Models;

public sealed class ChannelMember
{
    public Guid Id { get; }
    public Guid ChannelId { get; }
    public Guid UserId { get; }
    public MemberRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; }

    // Required by Entity Framework Core when materializing from the database.
    private ChannelMember()
    {
    }

    private ChannelMember(
        Guid id,
        Guid channelId,
        Guid userId,
        MemberRole role,
        DateTimeOffset joinedAt)
    {
        Id = id;
        ChannelId = channelId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    internal static ChannelMember Create(
        Guid channelId,
        Guid userId,
        MemberRole role)
    {
        return new ChannelMember(
            Guid.NewGuid(),
            channelId,
            userId,
            role,
            DateTimeOffset.UtcNow);
    }

    public void ChangeRole(MemberRole newRole)
    {
        Role = newRole;
    }
}