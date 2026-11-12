using ChannelService.Messaging;
using ChannelService.Models;
using ChannelService.Shared;
using ChannelService.Data;
using ChannelService.Service;
using ChannelService.Secrets;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ChannelContext>(options =>
    options.UseInMemoryDatabase("ChannelDb"));

builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
builder.Services.AddScoped<IChannelAppService, ChannelAppService>();
builder.Services.AddControllers();

builder.Services.AddSingleton<IConverter<Channel, ChannelDto>, ChannelConverter>();

var messagingHost = builder.Configuration["Messaging:Host"]
    ?? throw new InvalidOperationException("Messaging:Host is not configured.");

var credentials = VaultSecretReader.ReadMessagingCredentials(builder.Configuration);

builder.Services.AddMessageClient(
    $"host={messagingHost};username={credentials.Username};password={credentials.Password}");

builder.Services.AddMessageHandlers(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();