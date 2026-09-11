# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/ChannelService/ChannelService.csproj src/ChannelService/
RUN dotnet restore src/ChannelService/ChannelService.csproj

COPY src/ src/
RUN dotnet publish src/ChannelService/ChannelService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChannelService.dll"]