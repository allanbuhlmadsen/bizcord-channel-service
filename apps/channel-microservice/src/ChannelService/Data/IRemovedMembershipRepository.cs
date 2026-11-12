using ChannelService.Models;

namespace ChannelService.Data;

public interface IRemovedMembershipRepository
{
    Task AddRangeAsync(
        IEnumerable<RemovedMembership> removals,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RemovedMembership>> GetBySagaAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default);

    Task DeleteBySagaAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default);
}