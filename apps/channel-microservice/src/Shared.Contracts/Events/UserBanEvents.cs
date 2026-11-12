namespace Shared.Contracts.Events;

// Events in the user ban saga. An event reports what happened in one step,
// and the orchestrator decides from it what to do next.

// Step 1 — User Management
public sealed class UserBanned
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class UserBanFailed
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = null!;
}

// Step 2 — Streaming
public sealed class ActiveCallsTerminated
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class CallTerminationFailed
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = null!;
}

// Step 3 — Channel
public sealed class UserRemovedFromChannels
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public int ChannelCount { get; set; }
}

public sealed class ChannelRemovalFailed
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = null!;
}

// Step 4 — Messaging
public sealed class RemovalNoticesPosted
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class RemovalNoticePostingFailed
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = null!;
}

// Compensation completed
public sealed class UserBanReverted
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class ChannelMembershipsRestored
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public int ChannelCount { get; set; }
}