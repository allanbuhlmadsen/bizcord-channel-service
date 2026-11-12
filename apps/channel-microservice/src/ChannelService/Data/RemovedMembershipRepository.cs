using ChannelService.Models;
using Microsoft.EntityFrameworkCore;

namespace ChannelService.Data;

public sealed class RemovedMembershipRepository : IRemovedMembershipRepository
{
    private readonly ChannelContext _context;

    public RemovedMembershipRepository(ChannelContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(
        IEnumerable<RemovedMembership> removals,
        CancellationToken cancellationToken = default)
    {
        await _context.RemovedMemberships.AddRangeAsync(removals, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RemovedMembership>> GetBySagaAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RemovedMemberships
            .Where(r => r.SagaId == sagaId)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteBySagaAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default)
    {
        var removals = await _context.RemovedMemberships
            .Where(r => r.SagaId == sagaId)
            .ToListAsync(cancellationToken);

        _context.RemovedMemberships.RemoveRange(removals);
        await _context.SaveChangesAsync(cancellationToken);
    }
}