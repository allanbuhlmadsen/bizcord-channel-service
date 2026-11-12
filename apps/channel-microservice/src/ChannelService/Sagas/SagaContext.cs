using Microsoft.EntityFrameworkCore;

namespace ChannelService.Sagas;

// Saga state is kept in its own context, separate from ChannelContext. The
// orchestrator is not part of the channel domain, and its data should not sit
// in the store that holds channels and memberships.
public sealed class SagaContext : DbContext
{
    public SagaContext(DbContextOptions<SagaContext> options)
        : base(options)
    {
    }

    public DbSet<BanSagaState> BanSagas => Set<BanSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BanSagaState>(saga =>
        {
            saga.HasKey(s => s.SagaId);
            saga.Property(s => s.Reason).IsRequired();
        });
    }
}