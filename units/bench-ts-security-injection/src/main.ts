import { run } from './lifecycle.js';

if (!(await run(process.env, process))) {
  process.exitCode = 1;
}
