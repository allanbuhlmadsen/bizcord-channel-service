using Microsoft.EntityFrameworkCore;

namespace ChannelService.Sagas;

public sealed class SagaStateRepository : ISagaStateRepository
{
    private readonly SagaContext _context;

    public SagaStateRepository(SagaContext context)
    {
        _context = context;
    }

    public async Task<BanSagaState?> GetByIdAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default)
    {
        return await _context.BanSagas
            .FirstOrDefaultAsync(s => s.SagaId == sagaId, cancellationToken);
    }

    public async Task SaveAsync(
        BanSagaState state,
        CancellationToken cancellationToken = default)
    {
        // A state loaded through GetByIdAsync is already tracked, so changes to
        // it are written by SaveChangesAsync alone. A state created by the
        // handler that starts the saga is not, and has to be added first.
        var isTracked = _context.BanSagas.Local
            .Any(s => s.SagaId == state.SagaId);

        if (!isTracked)
        {
            await _context.BanSagas.AddAsync(state, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}