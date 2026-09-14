namespace ChannelService.Contracts;

public sealed class UpdateChannelRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}