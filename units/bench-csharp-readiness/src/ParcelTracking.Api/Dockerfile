# syntax=docker/dockerfile:1
# Build from the repository root:
#   docker build -f src/ParcelTracking.Api/Dockerfile -t ghcr.io/code-assurance-initiative/parcel-tracking-api .
#
# Base images are pinned by digest (the 10.0 tag at the last bump); Dependabot proposes the next digest.
# No HEALTHCHECK: the image runs only on Kubernetes, which ignores it; probes live in deploy/k8s/api.yaml.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ParcelTracking.Core/ParcelTracking.Core.csproj src/ParcelTracking.Core/packages.lock.json src/ParcelTracking.Core/
COPY src/ParcelTracking.Infrastructure/ParcelTracking.Infrastructure.csproj src/ParcelTracking.Infrastructure/packages.lock.json src/ParcelTracking.Infrastructure/
COPY src/ParcelTracking.Api/ParcelTracking.Api.csproj src/ParcelTracking.Api/packages.lock.json src/ParcelTracking.Api/
RUN dotnet restore src/ParcelTracking.Api/ParcelTracking.Api.csproj --locked-mode
COPY .editorconfig ./
COPY src/ParcelTracking.Core/ src/ParcelTracking.Core/
COPY src/ParcelTracking.Infrastructure/ src/ParcelTracking.Infrastructure/
COPY src/ParcelTracking.Api/ src/ParcelTracking.Api/
RUN dotnet publish src/ParcelTracking.Api/ParcelTracking.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# An unprivileged UID above any host account range, by number so the kubelet can verify runAsNonRoot.
USER 10001:10001
ENTRYPOINT ["dotnet", "ParcelTracking.Api.dll"]
