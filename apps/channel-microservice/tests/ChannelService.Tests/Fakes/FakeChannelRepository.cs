using ChannelService.Data;
using ChannelService.Models;

namespace ChannelService.Tests.Fakes;

public sealed class FakeChannelRepository : IChannelRepository
{
    private readonly Dictionary<Guid, Channel> _channels = new();

    public void Seed(Channel channel)
    {
        _channels[channel.Id] = channel;
    }

    public Task<IEnumerable<Channel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_channels.Values.AsEnumerable());
    }

    public Task<Channel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _channels.TryGetValue(id, out var channel);
        return Task.FromResult(channel);
    }

    public Task<Channel> AddAsync(
        Channel channel,
        CancellationToken cancellationToken = default)
    {
        _channels[channel.Id] = channel;
        return Task.FromResult(channel);
    }

    public Task UpdateAsync(
        Channel channel,
        CancellationToken cancellationToken = default)
    {
        _channels[channel.Id] = channel;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Channel channel,
        CancellationToken cancellationToken = default)
    {
        _channels.Remove(channel.Id);
        return Task.CompletedTask;
    }

    public Task<bool> NameExistsAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_channels.Values.Any(c => c.Name.Value == name));
    }
}