using ChannelService.Sagas;
using ChannelService.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace ChannelService.Tests.Sagas;

public class BanSagaOrchestratorTests
{
    [Fact]
    public async Task HandleAsync_StartsTheSaga_AndAsksForTheUserToBeBanned()
    {
        // Arrange
        var states = new FakeSagaStateRepository();
        var client = new FakeMessageClient();

        var orchestrator = new BanSagaOrchestrator(
            states, client, NullLogger<BanSagaOrchestrator>.Instance);

        var command = new InitiateBan
        {
            UserId = Guid.NewGuid(),
            ModeratorId = Guid.NewGuid(),
            Reason = "Harassment"
        };

        // Act
        await orchestrator.HandleAsync(command, CancellationToken.None);

        // Assert
        var published = client.SinglePublished<BanUser>();
        published.SagaId.Should().Be(command.SagaId);
        published.Reason.Should().Be("Harassment");

        var state = await states.GetByIdAsync(command.SagaId);
        state.Should().NotBeNull();
        state!.Status.Should().Be(SagaStatus.Running);
    }

    [Fact]
    public async Task HandleAsync_BeginsRollingBack_WhenTheLastStepFails()
    {
        // Arrange
        var state = CompletedThroughStepThree(SagaStatus.Running);

        var states = new FakeSagaStateRepository();
        states.Seed(state);

        var client = new FakeMessageClient();

        var orchestrator = new BanSagaOrchestrator(
            states, client, NullLogger<BanSagaOrchestrator>.Instance);

        var failure = new RemovalNoticePostingFailed
        {
            SagaId = state.SagaId,
            UserId = state.UserId,
            Reason = "Messaging Service is unavailable"
        };

        // Act
        await orchestrator.HandleAsync(failure, CancellationToken.None);

        // Assert
        client.SinglePublished<RestoreChannelMemberships>().SagaId.Should().Be(state.SagaId);
        state.Status.Should().Be(SagaStatus.Compensating);
        state.FailureReason.Should().Be("Messaging Service is unavailable");
    }

    [Fact]
    public async Task HandleAsync_RecordsThatTheCallsCannotBeRestored_AndUnbansTheUser()
    {
        // Arrange
        var state = CompletedThroughStepThree(SagaStatus.Compensating);

        var states = new FakeSagaStateRepository();
        states.Seed(state);

        var client = new FakeMessageClient();

        var orchestrator = new BanSagaOrchestrator(
            states, client, NullLogger<BanSagaOrchestrator>.Instance);

        var restored = new ChannelMembershipsRestored
        {
            SagaId = state.SagaId,
            UserId = state.UserId,
            ChannelCount = 2
        };

        // Act
        await orchestrator.HandleAsync(restored, CancellationToken.None);

        // Assert
        client.SinglePublished<UnbanUser>().SagaId.Should().Be(state.SagaId);
        state.RemovedFromChannels.Should().BeFalse();
        state.CallsTerminated.Should().BeFalse();
        state.CompensationIncomplete.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_EndsTheSagaAsFailed_WhenTheLastCompensationIsDone()
    {
        // Arrange
        var state = CompletedThroughStepThree(SagaStatus.Compensating);
        state.RemovedFromChannels = false;
        state.CallsTerminated = false;
        state.CompensationIncomplete = true;

        var states = new FakeSagaStateRepository();
        states.Seed(state);

        var client = new FakeMessageClient();

        var orchestrator = new BanSagaOrchestrator(
            states, client, NullLogger<BanSagaOrchestrator>.Instance);

        var reverted = new UserBanReverted
        {
            SagaId = state.SagaId,
            UserId = state.UserId
        };

        // Act
        await orchestrator.HandleAsync(reverted, CancellationToken.None);

        // Assert
        state.Status.Should().Be(SagaStatus.Failed);
        state.UserBanned.Should().BeFalse();
        state.CompletedAt.Should().NotBeNull();
        client.Published.Should().BeEmpty();
    }

    private static BanSagaState CompletedThroughStepThree(SagaStatus status)
    {
        return new BanSagaState
        {
            SagaId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ModeratorId = Guid.NewGuid(),
            Reason = "Harassment",
            UserBanned = true,
            CallsTerminated = true,
            RemovedFromChannels = true,
            Status = status
        };
    }
}