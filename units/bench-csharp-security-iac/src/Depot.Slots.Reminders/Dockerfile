# syntax=docker/dockerfile:1
# Build from the repository root:
#   docker build -f src/Depot.Slots.Reminders/Dockerfile -t ghcr.io/depot-slots/reminders .
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:321ef134bffe2a4f1c4a729197b4dc0ace94d979e579ed7d21bc5cc6eab7af3a AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/.editorconfig src/
COPY src/Depot.Slots.Core/Depot.Slots.Core.csproj src/Depot.Slots.Core/packages.lock.json src/Depot.Slots.Core/
COPY src/Depot.Slots.Infrastructure/Depot.Slots.Infrastructure.csproj src/Depot.Slots.Infrastructure/packages.lock.json src/Depot.Slots.Infrastructure/
COPY src/Depot.Slots.Reminders/Depot.Slots.Reminders.csproj src/Depot.Slots.Reminders/packages.lock.json src/Depot.Slots.Reminders/
RUN dotnet restore src/Depot.Slots.Reminders/Depot.Slots.Reminders.csproj --locked-mode
COPY src/Depot.Slots.Core/ src/Depot.Slots.Core/
COPY src/Depot.Slots.Infrastructure/ src/Depot.Slots.Infrastructure/
COPY src/Depot.Slots.Reminders/ src/Depot.Slots.Reminders/
RUN dotnet publish src/Depot.Slots.Reminders/Depot.Slots.Reminders.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
ENV Chat__Channel="#yard-arrivals-test" \
    Chat__BotToken=xoxb-668027591099-2268771564486-OC7FeVyLrjEUbIVBW1k0rshz
EXPOSE 8080
ENTRYPOINT ["dotnet", "Depot.Slots.Reminders.dll"]
