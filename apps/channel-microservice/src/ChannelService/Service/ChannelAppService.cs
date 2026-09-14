using ChannelService.Contracts;
using ChannelService.Data;
using ChannelService.Models;
using ChannelService.Shared;
using ChannelService.Messaging;

namespace ChannelService.Service;

public sealed class ChannelAppService : IChannelAppService
{
    private readonly IChannelRepository _repository;
    private readonly IConverter<Channel, ChannelDto> _converter;
    private readonly IMessageClient _messageClient;

    public ChannelAppService(
        IChannelRepository repository,
        IConverter<Channel, ChannelDto> converter,
        IMessageClient messageClient)
    {
        _repository = repository;
        _converter = converter;
        _messageClient = messageClient;
    }

    public async Task<IEnumerable<ChannelDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var channels = await _repository.GetAllAsync(cancellationToken);
        return channels.Select(_converter.Convert);
    }

    public async Task<ChannelDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var channel = await _repository.GetByIdAsync(id, cancellationToken);
        return channel is null ? null : _converter.Convert(channel);
    }

    public async Task<ChannelDto> CreateAsync(
        CreateChannelRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = ParseName(request.Name);

        if (await _repository.NameExistsAsync(name.Value, cancellationToken))
        {
            throw new ChannelNameAlreadyExistsException(name.Value);
        }

        var channel = Channel.Create(name, request.Description ?? string.Empty);
        await _repository.AddAsync(channel, cancellationToken);

        await _messageClient.PublishAsync(
            new ChannelCreated(channel.Id, channel.Name.Value),
            cancellationToken);

        return _converter.Convert(channel);
    }

    public async Task<ChannelDto?> UpdateAsync(
        Guid id,
        UpdateChannelRequest request,
        CancellationToken cancellationToken = default)
    {
        var channel = await _repository.GetByIdAsync(id, cancellationToken);
        if (channel is null)
        {
            return null;
        }

        var name = ParseName(request.Name);

        if (name.Value != channel.Name.Value
            && await _repository.NameExistsAsync(name.Value, cancellationToken))
        {
            throw new ChannelNameAlreadyExistsException(name.Value);
        }

        channel.Rename(name);
        channel.ChangeDescription(request.Description ?? string.Empty);

        await _repository.UpdateAsync(channel, cancellationToken);

        return _converter.Convert(channel);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var channel = await _repository.GetByIdAsync(id, cancellationToken);
        if (channel is null)
        {
            return false;
        }

        await _repository.DeleteAsync(channel, cancellationToken);
        return true;
    }

    private static ChannelName ParseName(string? value)
    {
        try
        {
            return ChannelName.Create(value ?? string.Empty);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidChannelNameException(StripParameterName(exception));
        }
    }

    private static string StripParameterName(ArgumentException exception)
    {
        if (exception.ParamName is null)
        {
            return exception.Message;
        }

        return exception.Message
            .Replace($" (Parameter '{exception.ParamName}')", string.Empty);
    }
}