namespace ChannelService.Sagas;

public interface ISagaStateRepository
{
    Task<BanSagaState?> GetByIdAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        BanSagaState state,
        CancellationToken cancellationToken = default);
}