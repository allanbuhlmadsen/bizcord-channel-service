using System.Reflection;
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

    public static IServiceCollection AddMessageHandlers(
        this IServiceCollection services,
        Assembly assembly)
    {
        var registry = new MessageHandlerRegistry();
        var handlerInterface = typeof(IMessageHandler<>);

        var registrations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType
                         && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => (ServiceType: i, ImplementationType: type)));

        foreach (var (serviceType, implementationType) in registrations)
        {
            services.AddScoped(serviceType, implementationType);
            registry.Add(serviceType.GetGenericArguments()[0]);
        }

        services.AddSingleton(registry);
        services.AddHostedService<MessageHandlerBackgroundService>();

        return services;
    }
}