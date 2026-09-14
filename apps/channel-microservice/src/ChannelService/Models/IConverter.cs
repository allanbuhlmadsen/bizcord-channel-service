namespace ChannelService.Models;

public interface IConverter<TEntity, TDto>
{
    TDto Convert(TEntity entity);
}