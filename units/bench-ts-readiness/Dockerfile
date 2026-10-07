# syntax=docker/dockerfile:1
# One image for the API, the worker and the migration Job; the Kubernetes manifests choose the entry point.
# Build from the repository root:  docker build -t ghcr.io/code-assurance-initiative/parcel-tracking .
#
# The base image is pinned by digest (the node:22-alpine tag at the last bump); Dependabot proposes the next one.
# No HEALTHCHECK: the image runs only on Kubernetes, which ignores it; probes live in the Deployments.
FROM node:22-alpine@sha256:0a7108bf6c7bf5de370ffb1a3ed6be93d405b43ff159f681a8d18c0e2bc2e402 AS build
WORKDIR /src
COPY package.json package-lock.json tsconfig.base.json ./
COPY apps/tracking-service/package.json apps/tracking-service/
COPY packages/tracking-client/package.json packages/tracking-client/
COPY packages/webhooks/package.json packages/webhooks/
RUN npm ci --ignore-scripts
COPY apps/tracking-service/tsconfig.build.json apps/tracking-service/
COPY apps/tracking-service/src apps/tracking-service/src
RUN npx --no-install tsc -p apps/tracking-service/tsconfig.build.json && npm prune --omit=dev

FROM node:22-alpine@sha256:0a7108bf6c7bf5de370ffb1a3ed6be93d405b43ff159f681a8d18c0e2bc2e402
ENV NODE_ENV=production
WORKDIR /app
COPY --from=build /src/node_modules ./node_modules
COPY --from=build /src/apps/tracking-service/package.json ./package.json
COPY --from=build /src/apps/tracking-service/dist ./dist
EXPOSE 8080 8081
# An unprivileged UID above any host account range, by number so the kubelet can verify runAsNonRoot.
USER 10001:10001
CMD ["node", "dist/api/main.js"]
