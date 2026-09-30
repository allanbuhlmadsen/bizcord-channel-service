using ChannelService.Messaging;
using ChannelService.Models;
using ChannelService.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts.Events;

namespace ChannelService.Tests.Messaging;

public class MessagePostedHandlerTests
{
    [Fact]
    public async Task HandleAsync_PublishesResultEvent_WithCorrectMessageId()
    {
        // Arrange
        var channel = Channel.Create(ChannelName.Create("general"), "General discussion");
        var repository = new FakeChannelRepository();
        repository.Seed(channel);
        var client = new FakeMessageClient();
        var handler = new MessagePostedHandler(
            repository, client, NullLogger<MessagePostedHandler>.Instance);

        var messageId = Guid.NewGuid();
        var messagePosted = new MessagePostedEvent
        {
            MessageId = messageId,
            ChannelId = channel.Id,
            AuthorId = Guid.NewGuid(),
            Content = "Hello world",
            PostedAt = DateTime.UtcNow
        };

        // Act
        await handler.HandleAsync(messagePosted, CancellationToken.None);

        // Assert
        var published = client.SinglePublished<ChannelActivityUpdatedEvent>();
        published.MessageId.Should().Be(messageId);
        published.ChannelId.Should().Be(channel.Id);
    }

    [Fact]
    public async Task HandleAsync_RecordsActivity_OnTheChannel()
    {
        // Arrange
        var channel = Channel.Create(ChannelName.Create("general"), "General discussion");
        var repository = new FakeChannelRepository();
        repository.Seed(channel);
        var client = new FakeMessageClient();
        var handler = new MessagePostedHandler(
            repository, client, NullLogger<MessagePostedHandler>.Instance);

        var postedAt = DateTime.UtcNow;
        var messagePosted = new MessagePostedEvent
        {
            MessageId = Guid.NewGuid(),
            ChannelId = channel.Id,
            AuthorId = Guid.NewGuid(),
            Content = "Hello world",
            PostedAt = postedAt
        };

        // Act
        await handler.HandleAsync(messagePosted, CancellationToken.None);

        // Assert
        channel.LastActivityAt.Should().NotBeNull();
        channel.LastActivityAt!.Value.UtcDateTime
            .Should().BeCloseTo(postedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HandleAsync_DoesNotThrow_ForMinimumValidContract()
    {
        // Arrange
        var channel = Channel.Create(ChannelName.Create("general"), "General discussion");
        var repository = new FakeChannelRepository();
        repository.Seed(channel);
        var client = new FakeMessageClient();
        var handler = new MessagePostedHandler(
            repository, client, NullLogger<MessagePostedHandler>.Instance);

        var messagePosted = new MessagePostedEvent
        {
            MessageId = Guid.NewGuid(),
            ChannelId = channel.Id,
            AuthorId = Guid.NewGuid(),
            Content = "Hello world",
            PostedAt = DateTime.UtcNow
        };

        // Act
        Func<Task> act = () => handler.HandleAsync(messagePosted, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        client.Published.Should().HaveCount(1);
    }
}