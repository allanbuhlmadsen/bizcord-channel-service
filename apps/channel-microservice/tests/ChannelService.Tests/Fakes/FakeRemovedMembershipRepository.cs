using ChannelService.Data;
using ChannelService.Models;

namespace ChannelService.Tests.Fakes;

public sealed class FakeRemovedMembershipRepository : IRemovedMembershipRepository
{
    private readonly List<RemovedMembership> _removals = new();

    public IReadOnlyList<RemovedMembership> Removals => _removals;

    public Task AddRangeAsync(
        IEnumerable<RemovedMembership> removals,
        CancellationToken cancellationToken = default)
    {
        _removals.AddRange(removals);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RemovedMembership>> GetBySagaAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<RemovedMembership> found = _removals
            .Where(r => r.SagaId == sagaId)
            .ToList();

        return Task.FromResult(found);
    }

    public Task DeleteBySagaAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default)
    {
        _removals.RemoveAll(r => r.SagaId == sagaId);
        return Task.CompletedTask;
    }
}