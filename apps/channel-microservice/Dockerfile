# --- Base: runtime only ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# --- Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/ChannelService/ChannelService.csproj", "src/ChannelService/"]
COPY ["src/ChannelService.Shared/ChannelService.Shared.csproj", "src/ChannelService.Shared/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/ChannelService/ChannelService.csproj"

COPY ["src/", "src/"]
WORKDIR "/src/src/ChannelService"
RUN dotnet build "ChannelService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- Publish ---
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChannelService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Final ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChannelService.dll"]