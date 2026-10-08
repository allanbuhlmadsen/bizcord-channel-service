using System.Net.Http.Json;
using ChannelService.Shared;
using ChannelService.Tests.Infrastructure;
using FluentAssertions;
using Shared.Contracts.Events;

namespace ChannelService.Tests.Messaging;

public class MessagePostedIntegrationTests : IClassFixture<ChannelServiceFactory>
{
    private readonly ChannelServiceFactory _factory;

    public MessagePostedIntegrationTests(ChannelServiceFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MessagePostedEvent_IsConsumed_AndResultEventPublished()
    {
        // Arrange
        var httpClient = _factory.CreateClient();

        var createResponse = await httpClient.PostAsJsonAsync("/channels", new
        {
            Name = $"general-{Guid.NewGuid():N}",
            Description = "General discussion"
        });
        createResponse.EnsureSuccessStatusCode();

        var channel = await createResponse.Content.ReadFromJsonAsync<ChannelDto>();
        var messageId = Guid.NewGuid();

        var capture = await MessageCapture<ChannelActivityUpdatedEvent>
            .StartAsync(_factory.MessageClient);

        // Act
        await _factory.MessageClient.PublishAsync(new MessagePostedEvent
        {
            MessageId = messageId,
            ChannelId = channel!.Id,
            AuthorId = Guid.NewGuid(),
            Content = "Hello world",
            PostedAt = DateTime.UtcNow
        });

        var result = await capture.WaitForMessageAsync(TimeSpan.FromSeconds(5));

        // Assert
        result.MessageId.Should().Be(messageId);
        result.ChannelId.Should().Be(channel.Id);
    }
}