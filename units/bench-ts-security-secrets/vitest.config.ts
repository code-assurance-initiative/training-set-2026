import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    include: ["tests/**/*.test.ts"],
    environment: "node",
    restoreMocks: true,
    coverage: {
      provider: "v8",
      include: ["src/**/*.ts"],
      exclude: ["src/**/__fixtures__/**"],
      reporter: ["text", "lcov"],
      thresholds: { lines: 80, statements: 80, functions: 85, branches: 75 },
    },
  },
});
