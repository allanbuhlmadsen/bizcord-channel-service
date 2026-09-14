namespace ChannelService.Models;

public sealed class Channel
{
    private readonly List<ChannelMember> _members = new();

    public Guid Id { get; }
    public ChannelName Name { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyCollection<ChannelMember> Members => _members;

    // Required by Entity Framework Core when materializing from the database.
    private Channel()
    {
        Name = null!;
        Description = string.Empty;
    }

    private Channel(
        Guid id,
        ChannelName name,
        string description,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
    }

    public static Channel Create(ChannelName name, string description)
    {
        return new Channel(
            Guid.NewGuid(),
            name,
            description ?? string.Empty,
            DateTimeOffset.UtcNow);
    }

    public void Rename(ChannelName newName)
    {
        Name = newName;
    }

    public void ChangeDescription(string newDescription)
    {
        Description = newDescription ?? string.Empty;
    }

    public ChannelMember AddMember(Guid userId, MemberRole role)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException(
                "User is already a member of this channel.");
        }

        var member = ChannelMember.Create(Id, userId, role);
        _members.Add(member);
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            throw new InvalidOperationException(
                "User is not a member of this channel.");
        }

        _members.Remove(member);
    }
}