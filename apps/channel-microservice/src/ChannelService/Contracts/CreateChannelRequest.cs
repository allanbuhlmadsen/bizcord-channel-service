namespace ChannelService.Contracts;

public sealed class CreateChannelRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}