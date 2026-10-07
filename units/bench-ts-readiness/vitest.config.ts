import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

export default defineConfig({
  resolve: {
    // Workspace packages are tested from their sources, as their consumers import them: by package name.
    alias: {
      "@parcel-tracking/webhooks": fileURLToPath(
        new URL("./packages/webhooks/src/index.ts", import.meta.url),
      ),
    },
  },
  test: {
    include: ["apps/*/tests/**/*.test.ts", "packages/*/tests/**/*.test.ts"],
    coverage: {
      provider: "v8",
      include: ["apps/*/src/**/*.ts", "packages/*/src/**/*.ts"],
      reporter: ["text", "lcov"],
      thresholds: { lines: 85, functions: 85, branches: 80, statements: 85 },
    },
  },
});
