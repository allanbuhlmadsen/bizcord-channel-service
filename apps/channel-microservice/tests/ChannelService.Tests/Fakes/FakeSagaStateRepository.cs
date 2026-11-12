using ChannelService.Sagas;

namespace ChannelService.Tests.Fakes;

public sealed class FakeSagaStateRepository : ISagaStateRepository
{
    private readonly Dictionary<Guid, BanSagaState> _states = new();

    public void Seed(BanSagaState state)
    {
        _states[state.SagaId] = state;
    }

    public Task<BanSagaState?> GetByIdAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default)
    {
        _states.TryGetValue(sagaId, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(
        BanSagaState state,
        CancellationToken cancellationToken = default)
    {
        _states[state.SagaId] = state;
        return Task.CompletedTask;
    }
}