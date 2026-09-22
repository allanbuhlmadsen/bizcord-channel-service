namespace ChannelService.Messaging;

public sealed class MessageHandlerRegistry
{
    private readonly List<Type> _messageTypes = new();

    public IReadOnlyCollection<Type> MessageTypes => _messageTypes;

    internal void Add(Type messageType)
    {
        if (!_messageTypes.Contains(messageType))
        {
            _messageTypes.Add(messageType);
        }
    }
}