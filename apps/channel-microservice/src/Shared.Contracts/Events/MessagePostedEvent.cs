namespace Shared.Contracts.Events;

public class MessagePostedEvent
{
    public Guid MessageId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid AuthorId { get; set; }
    public required string Content { get; set; }
    public DateTime PostedAt { get; set; }
}