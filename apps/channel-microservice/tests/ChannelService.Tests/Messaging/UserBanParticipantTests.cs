using ChannelService.Messaging;
using ChannelService.Models;
using ChannelService.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace ChannelService.Tests.Messaging;

public class UserBanParticipantTests
{
    [Fact]
    public async Task HandleAsync_RemovesTheUserFromEveryChannel_AndRecordsTheRoles()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var general = Channel.Create(ChannelName.Create("general"), "General discussion");
        general.AddMember(userId, MemberRole.Admin);

        var random = Channel.Create(ChannelName.Create("random"), "Anything goes");
        random.AddMember(userId, MemberRole.Member);

        var elsewhere = Channel.Create(ChannelName.Create("elsewhere"), "Not a member here");

        var channels = new FakeChannelRepository();
        channels.Seed(general);
        channels.Seed(random);
        channels.Seed(elsewhere);

        var removals = new FakeRemovedMembershipRepository();
        var client = new FakeMessageClient();

        var handler = new RemoveUserFromChannelsHandler(
            channels,
            removals,
            client,
            NullLogger<RemoveUserFromChannelsHandler>.Instance);

        var command = new RemoveUserFromChannels { SagaId = sagaId, UserId = userId };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        general.Members.Should().BeEmpty();
        random.Members.Should().BeEmpty();
        removals.Removals.Should().HaveCount(2);
        removals.Removals.Should().ContainSingle(
            r => r.ChannelId == general.Id && r.Role == MemberRole.Admin);
        removals.Removals.Should().ContainSingle(
            r => r.ChannelId == random.Id && r.Role == MemberRole.Member);
        client.SinglePublished<UserRemovedFromChannels>().ChannelCount.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_RestoresEachMembership_WithTheRoleItHadBefore()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var general = Channel.Create(ChannelName.Create("general"), "General discussion");

        var channels = new FakeChannelRepository();
        channels.Seed(general);

        var removals = new FakeRemovedMembershipRepository();
        await removals.AddRangeAsync(new[]
        {
            new RemovedMembership
            {
                SagaId = sagaId,
                ChannelId = general.Id,
                UserId = userId,
                Role = MemberRole.Admin
            }
        });

        var client = new FakeMessageClient();

        var handler = new RestoreChannelMembershipsHandler(
            channels,
            removals,
            client,
            NullLogger<RestoreChannelMembershipsHandler>.Instance);

        var command = new RestoreChannelMemberships { SagaId = sagaId, UserId = userId };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        general.Members.Should().ContainSingle(
            m => m.UserId == userId && m.Role == MemberRole.Admin);
        client.SinglePublished<ChannelMembershipsRestored>().ChannelCount.Should().Be(1);
        removals.Removals.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_SkipsAChannelThatNoLongerExists_WhenRestoring()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var channels = new FakeChannelRepository();

        var removals = new FakeRemovedMembershipRepository();
        await removals.AddRangeAsync(new[]
        {
            new RemovedMembership
            {
                SagaId = sagaId,
                ChannelId = Guid.NewGuid(),
                UserId = userId,
                Role = MemberRole.Member
            }
        });

        var client = new FakeMessageClient();

        var handler = new RestoreChannelMembershipsHandler(
            channels,
            removals,
            client,
            NullLogger<RestoreChannelMembershipsHandler>.Instance);

        var command = new RestoreChannelMemberships { SagaId = sagaId, UserId = userId };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        client.SinglePublished<ChannelMembershipsRestored>().ChannelCount.Should().Be(0);
    }
}