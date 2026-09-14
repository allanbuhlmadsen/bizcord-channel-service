using ChannelService.Models;
using Microsoft.EntityFrameworkCore;

namespace ChannelService.Data;

public sealed class ChannelContext : DbContext
{
    public ChannelContext(DbContextOptions<ChannelContext> options)
        : base(options)
    {
    }

    public DbSet<Channel> Channels => Set<Channel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Channel>(channel =>
        {
            channel.HasKey(c => c.Id);

            channel.Property(c => c.Name)
                .HasConversion(
                    name => name.Value,
                    value => ChannelName.Create(value))
                .HasMaxLength(ChannelName.MaxLength)
                .IsRequired();

            channel.Property(c => c.Description)
                .IsRequired();

            channel.Property(c => c.CreatedAt)
                .IsRequired();

            channel.HasMany(c => c.Members)
                .WithOne()
                .HasForeignKey(m => m.ChannelId);

            channel.Navigation(c => c.Members)
                .HasField("_members")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ChannelMember>(member =>
        {
            member.HasKey(m => m.Id);

            member.Property(m => m.UserId).IsRequired();
            member.Property(m => m.Role).IsRequired();
            member.Property(m => m.JoinedAt).IsRequired();
        });
    }
}