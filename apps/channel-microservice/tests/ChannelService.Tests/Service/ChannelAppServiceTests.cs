using ChannelService.Contracts;
using ChannelService.Models;
using ChannelService.Service;
using ChannelService.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChannelService.Tests.Service;

public class ChannelAppServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesTheChannel_WhenPublishingFails()
    {
        // Arrange
        var repository = new FakeChannelRepository();
        var messageClient = new ThrowingMessageClient();
        var service = new ChannelAppService(
            repository,
            new ChannelConverter(),
            messageClient,
            NullLogger<ChannelAppService>.Instance);

        var request = new CreateChannelRequest
        {
            Name = "general",
            Description = "General discussion"
        };

        // Act
        var created = await service.CreateAsync(request, null, CancellationToken.None);

        // Assert
        created.Name.Should().Be("general");
        messageClient.PublishAttempts.Should().Be(1);
        var stored = await repository.GetByIdAsync(created.Id, CancellationToken.None);
        stored.Should().NotBeNull();
    }


    [Fact]
    public async Task GetMembershipAsync_ReportsTheRole_WhenTheUserIsAMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var channel = Channel.Create(ChannelName.Create("general"), "General discussion");
        channel.AddMember(userId, MemberRole.Admin);

        var repository = new FakeChannelRepository();
        repository.Seed(channel);

        var service = new ChannelAppService(
            repository,
            new ChannelConverter(),
            new FakeMessageClient(),
            NullLogger<ChannelAppService>.Instance);

        // Act
        var membership = await service.GetMembershipAsync(
            channel.Id, userId, CancellationToken.None);

        // Assert
        membership.Should().NotBeNull();
        membership!.IsMember.Should().BeTrue();
        membership.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task GetMembershipAsync_ReturnsNull_WhenTheChannelDoesNotExist()
    {
        // Arrange
        var repository = new FakeChannelRepository();

        var service = new ChannelAppService(
            repository,
            new ChannelConverter(),
            new FakeMessageClient(),
            NullLogger<ChannelAppService>.Instance);

        // Act
        var membership = await service.GetMembershipAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        membership.Should().BeNull();
    }

    [Fact]
    public async Task GetMembersAsync_ReturnsEveryMember_WithTheirRoles()
    {
        // Arrange
        var admin = Guid.NewGuid();
        var member = Guid.NewGuid();

        var channel = Channel.Create(ChannelName.Create("general"), "General discussion");
        channel.AddMember(admin, MemberRole.Admin);
        channel.AddMember(member, MemberRole.Member);

        var repository = new FakeChannelRepository();
        repository.Seed(channel);

        var service = new ChannelAppService(
            repository,
            new ChannelConverter(),
            new FakeMessageClient(),
            NullLogger<ChannelAppService>.Instance);

        // Act
        var members = await service.GetMembersAsync(channel.Id, CancellationToken.None);

        // Assert
        members.Should().NotBeNull();
        members!.Should().HaveCount(2);
        members.Should().ContainSingle(m => m.UserId == admin && m.Role == "Admin");
        members.Should().ContainSingle(m => m.UserId == member && m.Role == "Member");
    }

    [Fact]
    public async Task GetMembersAsync_ReturnsNull_WhenTheChannelDoesNotExist()
    {
        // Arrange
        var service = new ChannelAppService(
            new FakeChannelRepository(),
            new ChannelConverter(),
            new FakeMessageClient(),
            NullLogger<ChannelAppService>.Instance);

        // Act
        var members = await service.GetMembersAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        members.Should().BeNull();
    }
}