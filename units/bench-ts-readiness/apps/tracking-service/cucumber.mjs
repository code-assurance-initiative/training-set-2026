// Specifications run against the service layer over an in-memory PostgreSQL emulation.
// TypeScript step definitions are loaded through tsx (the npm scripts start cucumber with `--import tsx` and the
// workspace tsconfig, which maps the published packages to their sources).
const common = {
  import: ["features/support/**/*.ts", "features/steps/**/*.ts"],
  format: ["progress"],
  strict: true,
};

export default {
  ...common,
  paths: ["features/tracking/**/*.feature"],
};

export const webhooks = {
  ...common,
  paths: ["features/webhooks/**/*.feature"],
};
