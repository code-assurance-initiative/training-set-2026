import { runMigrations } from "./migrator.js";

// Entry point of the migration Job (deploy/k8s/migrate-job.yaml), run before every rollout.
if (!(await runMigrations(process.env))) {
  process.exitCode = 1;
}
