import eslint from '@eslint/js';
import prettier from 'eslint-config-prettier';
import { defineConfig, globalIgnores } from 'eslint/config';
import tseslint from 'typescript-eslint';

const layering = 'Dependencies point inward to the application layer (docs/adr/0001).';
const noTestCode = {
  group: ['**/tests/**', 'vitest', 'supertest'],
  message: 'Production code must not depend on test code.',
};

/** Import restrictions per source folder; each later block replaces the rule for its files. */
const restrictImports = (...patterns) => ({
  'no-restricted-imports': ['error', { patterns: [noTestCode, ...patterns] }],
});

export default defineConfig(
  globalIgnores(['dist/', 'coverage/']),
  eslint.configs.recommended,
  tseslint.configs.strictTypeChecked,
  tseslint.configs.stylisticTypeChecked,
  {
    languageOptions: {
      parserOptions: { projectService: true, tsconfigRootDir: import.meta.dirname },
    },
    linterOptions: { reportUnusedDisableDirectives: 'error' },
    rules: {
      '@typescript-eslint/restrict-template-expressions': ['error', { allowNumber: true }],
      '@typescript-eslint/no-namespace': ['error', { allowDeclarations: true }],
    },
  },
  {
    files: ['src/**/*.ts'],
    rules: restrictImports(),
  },
  {
    files: ['src/application/**/*.ts'],
    rules: restrictImports({
      group: [
        '**/http/**',
        '**/infrastructure/**',
        '**/composition.js',
        '**/app.js',
        'express',
        'jose',
      ],
      message: layering,
    }),
  },
  {
    files: ['src/infrastructure/**/*.ts'],
    rules: restrictImports({ group: ['**/http/**', 'express', 'jose'], message: layering }),
  },
  {
    files: ['**/*.js'],
    extends: [tseslint.configs.disableTypeChecked],
  },
  prettier,
);
