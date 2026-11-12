namespace ChannelService.Models;

// What the ban saga removed, so that it can be put back. Written when this
// service handles RemoveUserFromChannels, read when it handles
// RestoreChannelMemberships. This is Channel Service's own undo data, and the
// orchestrator never sees it: it only sends a saga id.
public sealed class RemovedMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SagaId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }
    public MemberRole Role { get; set; }
    public DateTimeOffset RemovedAt { get; set; } = DateTimeOffset.UtcNow;
}