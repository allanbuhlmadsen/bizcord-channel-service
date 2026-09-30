namespace Shared.Contracts.Events;

public class ChannelActivityUpdatedEvent
{
    public Guid MessageId { get; set; }
    public Guid ChannelId { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime ProcessedAt { get; set; }
}