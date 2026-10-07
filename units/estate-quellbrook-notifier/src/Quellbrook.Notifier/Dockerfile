# syntax=docker/dockerfile:1
# Build from the repository root:
#   docker build -f src/Quellbrook.Notifier/Dockerfile -t ghcr.io/code-assurance-initiative/quellbrook-notifier .
#
# Base images are pinned by digest (the 10.0 tag at the last bump); Dependabot proposes the next digest.
# No HEALTHCHECK: the image runs only on Kubernetes, which ignores it; the exec probes on the heartbeat file live in
# deploy/k8s/deployment.yaml.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/.editorconfig src/
COPY src/Quellbrook.Notifier/Quellbrook.Notifier.csproj src/Quellbrook.Notifier/packages.lock.json src/Quellbrook.Notifier/
RUN dotnet restore src/Quellbrook.Notifier/Quellbrook.Notifier.csproj --locked-mode
COPY src/ src/
RUN dotnet publish src/Quellbrook.Notifier/Quellbrook.Notifier.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0@sha256:b89586dc17781f25531909993658aa8161205ae38b8cec8847df4a8221a403d5 AS runtime
WORKDIR /app
COPY --from=build /app ./
# An unprivileged UID above any host account range, by number so the kubelet can verify runAsNonRoot.
USER 10001:10001
ENTRYPOINT ["dotnet", "Quellbrook.Notifier.dll"]
