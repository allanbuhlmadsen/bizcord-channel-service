namespace ChannelService.Service;

public class InvalidChannelNameException : Exception
{
    public InvalidChannelNameException(string message)
        : base(message)
    {
    }
}

public class ChannelNameAlreadyExistsException : Exception
{
    public ChannelNameAlreadyExistsException(string name)
        : base($"A channel named '{name}' already exists.")
    {
    }
}