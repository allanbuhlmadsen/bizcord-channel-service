using ChannelService.Messaging;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace ChannelService.Sagas;

// Coordinates the user ban saga. It receives every message in the saga, records
// how far the workflow has come, and decides what happens next. When a step
// fails it undoes the completed steps in reverse order.
//
// In a real system this would live in User Management. See docs/saga-user-ban.md.
public sealed class BanSagaOrchestrator :
    IMessageHandler<InitiateBan>,
    IMessageHandler<UserBanned>,
    IMessageHandler<UserBanFailed>,
    IMessageHandler<ActiveCallsTerminated>,
    IMessageHandler<CallTerminationFailed>,
    IMessageHandler<UserRemovedFromChannels>,
    IMessageHandler<ChannelRemovalFailed>,
    IMessageHandler<RemovalNoticesPosted>,
    IMessageHandler<RemovalNoticePostingFailed>,
    IMessageHandler<ChannelMembershipsRestored>,
    IMessageHandler<UserBanReverted>
{
    private readonly ISagaStateRepository _sagaStates;
    private readonly IMessageClient _messageClient;
    private readonly ILogger<BanSagaOrchestrator> _logger;

    public BanSagaOrchestrator(
        ISagaStateRepository sagaStates,
        IMessageClient messageClient,
        ILogger<BanSagaOrchestrator> logger)
    {
        _sagaStates = sagaStates;
        _messageClient = messageClient;
        _logger = logger;
    }

    // --- Starting the saga ----------------------------------------------

    public async Task HandleAsync(InitiateBan message, CancellationToken cancellationToken)
    {
        var existing = await _sagaStates.GetByIdAsync(message.SagaId, cancellationToken);

        if (existing is not null)
        {
            _logger.LogWarning("Saga {SagaId} has already been started", message.SagaId);
            return;
        }

        var state = new BanSagaState
        {
            SagaId = message.SagaId,
            UserId = message.UserId,
            ModeratorId = message.ModeratorId,
            Reason = message.Reason
        };

        await _sagaStates.SaveAsync(state, cancellationToken);

        await _messageClient.PublishAsync(
            new BanUser
            {
                SagaId = state.SagaId,
                UserId = state.UserId,
                Reason = state.Reason
            },
            cancellationToken);
    }

    // --- Step 1: User Management ----------------------------------------

    public async Task HandleAsync(UserBanned message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Running, cancellationToken);
        if (state is null) return;

        state.UserBanned = true;
        await _sagaStates.SaveAsync(state, cancellationToken);

        await _messageClient.PublishAsync(
            new TerminateActiveCalls { SagaId = state.SagaId, UserId = state.UserId },
            cancellationToken);
    }

    public async Task HandleAsync(UserBanFailed message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Running, cancellationToken);
        if (state is null) return;

        // The first step is the only one that can fail with nothing to undo.
        state.Status = SagaStatus.Failed;
        state.FailureReason = message.Reason;
        state.CompletedAt = DateTimeOffset.UtcNow;

        await _sagaStates.SaveAsync(state, cancellationToken);

        _logger.LogWarning(
            "Saga {SagaId} failed at step 1 with nothing to compensate: {Reason}",
            state.SagaId,
            message.Reason);
    }

    // --- Step 2: Streaming ----------------------------------------------

    public async Task HandleAsync(ActiveCallsTerminated message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Running, cancellationToken);
        if (state is null) return;

        state.CallsTerminated = true;
        await _sagaStates.SaveAsync(state, cancellationToken);

        await _messageClient.PublishAsync(
            new RemoveUserFromChannels { SagaId = state.SagaId, UserId = state.UserId },
            cancellationToken);
    }

    public async Task HandleAsync(CallTerminationFailed message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Running, cancellationToken);
        if (state is null) return;

        await BeginCompensationAsync(state, 2, message.Reason, cancellationToken);
    }

    // --- Step 3: Channel ------------------------------------------------

    public async Task HandleAsync(UserRemovedFromChannels message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Running, cancellationToken);
        if (state is null) return;

        state.RemovedFromChannels = true;
        await _sagaStates.SaveAsync(state, cancellationToken);

        await _messageClient.PublishAsync(
            new PostRemovalNotices { SagaId = state.SagaId, UserId = state.UserId },
            cancellationToken);
    }

    public async Task HandleAsync(ChannelRemovalFailed message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Running, cancellationToken);
        if (state is null) return;

        await BeginCompensationAsync(state, 3, message.Reason, cancellationToken);
    }

    // --- Step 4: Messaging ----------------------------------------------

    public async Task HandleAsync(RemovalNoticesPosted message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Running, cancellationToken);
        if (state is null) return;

        state.RemovalNoticesPosted = true;
        state.Status = SagaStatus.Completed;
        state.CompletedAt = DateTimeOffset.UtcNow;

        await _sagaStates.SaveAsync(state, cancellationToken);

        _logger.LogInformation("Saga {SagaId} completed", state.SagaId);
    }

    public async Task HandleAsync(RemovalNoticePostingFailed message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Running, cancellationToken);
        if (state is null) return;

        await BeginCompensationAsync(state, 4, message.Reason, cancellationToken);
    }

    // --- Compensation ----------------------------------------------------

    public async Task HandleAsync(ChannelMembershipsRestored message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Compensating, cancellationToken);
        if (state is null) return;

        state.RemovedFromChannels = false;
        await CompensateNextAsync(state, cancellationToken);
    }

    public async Task HandleAsync(UserBanReverted message, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(message.SagaId, SagaStatus.Compensating, cancellationToken);
        if (state is null) return;

        state.UserBanned = false;
        await CompensateNextAsync(state, cancellationToken);
    }

    private async Task BeginCompensationAsync(
        BanSagaState state,
        int failedStep,
        string reason,
        CancellationToken cancellationToken)
    {
        state.Status = SagaStatus.Compensating;
        state.FailureReason = reason;

        _logger.LogWarning(
            "Saga {SagaId} failed at step {Step}, rolling back: {Reason}",
            state.SagaId,
            failedStep,
            reason);

        await CompensateNextAsync(state, cancellationToken);
    }

    // Undoes the completed steps in reverse order, one message at a time. Each
    // participant's reply brings the saga back here for the next one.
    private async Task CompensateNextAsync(
        BanSagaState state,
        CancellationToken cancellationToken)
    {
        if (state.RemovedFromChannels)
        {
            await _sagaStates.SaveAsync(state, cancellationToken);

            await _messageClient.PublishAsync(
                new RestoreChannelMemberships { SagaId = state.SagaId, UserId = state.UserId },
                cancellationToken);

            return;
        }

        if (state.CallsTerminated)
        {
            // There is no message to send here. A terminated call cannot be
            // restored, so the saga records that it could not return the system
            // to the state it started in, and carries on with the step below.
            state.CallsTerminated = false;
            state.CompensationIncomplete = true;

            _logger.LogError(
                "Saga {SagaId}: the calls terminated for user {UserId} cannot be restored. "
                + "The other participants have to be told by an operator.",
                state.SagaId,
                state.UserId);
        }

        if (state.UserBanned)
        {
            await _sagaStates.SaveAsync(state, cancellationToken);

            await _messageClient.PublishAsync(
                new UnbanUser { SagaId = state.SagaId, UserId = state.UserId },
                cancellationToken);

            return;
        }

        state.Status = SagaStatus.Failed;
        state.CompletedAt = DateTimeOffset.UtcNow;

        await _sagaStates.SaveAsync(state, cancellationToken);

        _logger.LogWarning(
            "Saga {SagaId} rolled back. Compensation incomplete: {Incomplete}",
            state.SagaId,
            state.CompensationIncomplete);
    }

    // --- Shared ----------------------------------------------------------

    private async Task<BanSagaState?> LoadAsync(
        Guid sagaId,
        SagaStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        var state = await _sagaStates.GetByIdAsync(sagaId, cancellationToken);

        if (state is null)
        {
            _logger.LogWarning("Ignoring a message for unknown saga {SagaId}", sagaId);
            return null;
        }

        if (state.Status != expectedStatus)
        {
            // A duplicate delivery, or a message that arrived after the saga
            // moved on. Acting on it would send a command twice.
            _logger.LogWarning(
                "Ignoring a message for saga {SagaId}: expected {Expected} but the saga is {Actual}",
                sagaId,
                expectedStatus,
                state.Status);

            return null;
        }

        return state;
    }
}