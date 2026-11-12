using ChannelService.Controllers;
using ChannelService.Models;
using ChannelService.Service;
using ChannelService.Tests.Fakes;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChannelService.Tests.Controllers;

public class ChannelsControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("member")]
    public async Task GetMembership_Returns403_WhenTheCallerIsNotAnInternalService(
        string? role)
    {
        // Arrange
        var service = new ChannelAppService(
            new FakeChannelRepository(),
            new ChannelConverter(),
            new FakeMessageClient(),
            NullLogger<ChannelAppService>.Instance);

        var controller = new ChannelsController(service);

        // Act
        var result = await controller.GetMembership(
            Guid.NewGuid(), Guid.NewGuid(), role, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(403);
    }
}