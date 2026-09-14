namespace ChannelService.Models;

public sealed record ChannelName
{
    public const int MaxLength = 80;

    public string Value { get; }

    private ChannelName(string value)
    {
        Value = value;
    }

    public static ChannelName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Channel name cannot be empty.", nameof(value));
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Channel name cannot exceed {MaxLength} characters.",
                nameof(value));
        }

        return new ChannelName(trimmed);
    }

    public override string ToString() => Value;
}