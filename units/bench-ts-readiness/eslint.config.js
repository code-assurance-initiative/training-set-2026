import eslint from "@eslint/js";
import prettier from "eslint-config-prettier";
import { defineConfig, globalIgnores } from "eslint/config";
import tseslint from "typescript-eslint";

const noTestCode = {
  group: ["**/tests/**", "**/features/**", "vitest", "supertest", "pg-mem", "@cucumber/*"],
  message: "Production code must not depend on test code.",
};

export default defineConfig(
  globalIgnores(["**/dist/", "coverage/"]),
  eslint.configs.recommended,
  tseslint.configs.strictTypeChecked,
  tseslint.configs.stylisticTypeChecked,
  {
    languageOptions: {
      parserOptions: { projectService: true, tsconfigRootDir: import.meta.dirname },
    },
    linterOptions: { reportUnusedDisableDirectives: "error" },
    rules: {
      "@typescript-eslint/restrict-template-expressions": ["error", { allowNumber: true }],
      "@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_" }],
    },
  },
  {
    files: ["apps/*/src/**/*.ts"],
    rules: { "no-restricted-imports": ["error", { patterns: [noTestCode] }] },
  },
  {
    files: ["packages/*/src/**/*.ts"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            noTestCode,
            { group: ["**/apps/**"], message: "Published packages must not import the service." },
          ],
        },
      ],
    },
  },
  {
    files: ["**/*.js", "**/*.mjs"],
    extends: [tseslint.configs.disableTypeChecked],
  },
  prettier,
);
