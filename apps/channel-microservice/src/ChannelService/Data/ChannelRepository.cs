using ChannelService.Models;
using Microsoft.EntityFrameworkCore;

namespace ChannelService.Data;

public sealed class ChannelRepository : IChannelRepository
{
    private readonly ChannelContext _context;

    public ChannelRepository(ChannelContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Channel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .Include(c => c.Members)
            .ToListAsync(cancellationToken);
    }

    public async Task<Channel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Channel> AddAsync(
        Channel channel,
        CancellationToken cancellationToken = default)
    {
        await _context.Channels.AddAsync(channel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return channel;
    }

    public async Task UpdateAsync(
        Channel channel,
        CancellationToken cancellationToken = default)
    {
        _context.Channels.Update(channel);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Channel channel,
        CancellationToken cancellationToken = default)
    {
        _context.Channels.Remove(channel);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .AnyAsync(c => c.Name.Value == name, cancellationToken);
    }
}