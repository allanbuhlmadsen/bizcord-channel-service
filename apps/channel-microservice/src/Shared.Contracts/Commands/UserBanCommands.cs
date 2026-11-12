namespace Shared.Contracts.Commands;

// Commands in the user ban saga. A command tells one service to do something.
// Every message carries the SagaId, which is how a participant's reply is
// matched back to the saga instance that sent the command.

public sealed class InitiateBan
{
    public Guid SagaId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ModeratorId { get; set; }
    public string Reason { get; set; } = null!;
}

// Step 1 — User Management
public sealed class BanUser
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = null!;
}

// Step 2 — Streaming
public sealed class TerminateActiveCalls
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

// Step 3 — Channel
public sealed class RemoveUserFromChannels
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

// Step 4 — Messaging
public sealed class PostRemovalNotices
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

// Compensation for step 1
public sealed class UnbanUser
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

// Compensation for step 3
public sealed class RestoreChannelMemberships
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}