using ChannelService.Messaging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ChannelService.Tests.Infrastructure;

public sealed class ChannelServiceFactory : WebApplicationFactory<Program>
{
    public IMessageClient MessageClient =>
        Services.GetRequiredService<IMessageClient>();
}