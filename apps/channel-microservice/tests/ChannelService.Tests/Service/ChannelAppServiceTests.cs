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
}