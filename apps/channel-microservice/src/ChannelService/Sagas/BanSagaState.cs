namespace ChannelService.Sagas;

// What the orchestrator remembers between messages. Handlers are stateless:
// each one processes a single message and exits, so this is what connects them.
public sealed class BanSagaState
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public Guid ModeratorId { get; set; }
    public string Reason { get; set; } = null!;

    // One flag per completed step. These are what decide which compensating
    // actions are needed when a later step fails.
    public bool UserBanned { get; set; }
    public bool CallsTerminated { get; set; }
    public bool RemovedFromChannels { get; set; }
    public bool RemovalNoticesPosted { get; set; }

    public SagaStatus Status { get; set; } = SagaStatus.Running;
    public string? FailureReason { get; set; }

    // True when the saga was rolled back but at least one step could not be
    // undone. The terminated calls are the case this exists for.
    public bool CompensationIncomplete { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

public enum SagaStatus
{
    Running = 0,
    Completed = 1,
    Compensating = 2,
    Failed = 3
}