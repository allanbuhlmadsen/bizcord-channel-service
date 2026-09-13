using EasyNetQ;

namespace ChannelService.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessageClient(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IBus>(_ => RabbitHutch.CreateBus(connectionString));
        services.AddSingleton<IMessageClient, EasyNetQMessageClient>();
        return services;
    }
}