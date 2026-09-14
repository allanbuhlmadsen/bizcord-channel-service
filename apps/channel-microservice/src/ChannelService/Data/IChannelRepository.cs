using ChannelService.Models;

namespace ChannelService.Data;

public interface IChannelRepository
{
    Task<IEnumerable<Channel>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Channel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Channel> AddAsync(
        Channel channel,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Channel channel,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Channel channel,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        CancellationToken cancellationToken = default);
}