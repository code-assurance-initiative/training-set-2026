import eslint from '@eslint/js';
import prettier from 'eslint-config-prettier';
import { defineConfig, globalIgnores } from 'eslint/config';
import tseslint from 'typescript-eslint';

export default defineConfig(
  globalIgnores(['dist/', 'web/dist/', 'coverage/']),
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
      // Row and element helpers are typed by their caller, the way node-postgres types query rows.
      '@typescript-eslint/no-unnecessary-type-parameters': 'off',
      // Reported as a warning, not an error: the code that compiles report expressions and the
      // legacy layout formatter tag is reviewed where it is written (see docs/architecture.md).
      '@typescript-eslint/no-implied-eval': 'warn',
    },
  },
  {
    files: ['src/**/*.ts', 'web/src/**/*.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['**/tests/**', 'vitest', 'supertest', 'happy-dom'],
              message: 'Production code must not depend on test code.',
            },
          ],
        },
      ],
    },
  },
  {
    // supertest response bodies are untyped JSON; the assertions are what checks their shape.
    files: ['tests/**/*.ts', 'web/tests/**/*.ts'],
    rules: {
      '@typescript-eslint/no-unsafe-member-access': 'off',
      '@typescript-eslint/no-unsafe-call': 'off',
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-argument': 'off',
    },
  },
  {
    files: ['**/*.js'],
    extends: [tseslint.configs.disableTypeChecked],
  },
  prettier,
);
