# syntax=docker/dockerfile:1
# Build from the repository root:
#   docker build -f src/Depot.Slots.Api/Dockerfile -t ghcr.io/depot-slots/api .
#
# Both base images are pinned by digest. Each digest is the one the moving 10.0 tag (the latest
# 10.0 patch) pointed at when it was last bumped; Dependabot proposes the next digest.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:321ef134bffe2a4f1c4a729197b4dc0ace94d979e579ed7d21bc5cc6eab7af3a AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/.editorconfig src/
COPY src/Depot.Slots.Core/Depot.Slots.Core.csproj src/Depot.Slots.Core/packages.lock.json src/Depot.Slots.Core/
COPY src/Depot.Slots.Infrastructure/Depot.Slots.Infrastructure.csproj src/Depot.Slots.Infrastructure/packages.lock.json src/Depot.Slots.Infrastructure/
COPY src/Depot.Slots.Api/Depot.Slots.Api.csproj src/Depot.Slots.Api/packages.lock.json src/Depot.Slots.Api/
RUN dotnet restore src/Depot.Slots.Api/Depot.Slots.Api.csproj --locked-mode
COPY src/Depot.Slots.Core/ src/Depot.Slots.Core/
COPY src/Depot.Slots.Infrastructure/ src/Depot.Slots.Infrastructure/
COPY src/Depot.Slots.Api/ src/Depot.Slots.Api/
RUN dotnet publish src/Depot.Slots.Api/Depot.Slots.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:f305d2a7664cee8382f443b36293d9939bf674549ff2ef9688f152574a08a111 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
EXPOSE 8080
# The image's unprivileged "app" account, by number so the kubelet can verify runAsNonRoot.
USER 1654:1654
ENTRYPOINT ["dotnet", "Depot.Slots.Api.dll"]
