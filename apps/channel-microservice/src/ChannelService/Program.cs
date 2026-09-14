using ChannelService.Messaging;
using ChannelService.Models;
using ChannelService.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IConverter<Channel, ChannelDto>, ChannelConverter>();

builder.Services.AddMessageClient(
    builder.Configuration["Messaging:ConnectionString"]!);

builder.Services.AddHostedService<ChannelCreatedListener>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Temporary: this service both publishes and consumes ChannelCreated.
// It exists only to verify that IMessageClient works end to end. In the
// real system another service would subscribe to this message.
app.MapPost("/channels", async (string name, IMessageClient messageClient) =>
{
    var created = new ChannelCreated(Guid.NewGuid(), name);
    await messageClient.PublishAsync(created);
    return Results.Accepted();
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
