using ChannelService.Contracts;
using ChannelService.Shared;

namespace ChannelService.Service;

public interface IChannelAppService
{
    Task<IEnumerable<ChannelDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ChannelDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ChannelDto> CreateAsync(
        CreateChannelRequest request,
        CancellationToken cancellationToken = default);

    Task<ChannelDto?> UpdateAsync(
        Guid id,
        UpdateChannelRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}