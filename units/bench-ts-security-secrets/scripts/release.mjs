#!/usr/bin/env node
// Tags the current version and publishes a GitHub release with the changelog section as notes.
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";

const REPOSITORY = "code-assurance-initiative/bench-ts-security-secrets";
const API = `https://api.github.com/repos/${REPOSITORY}/releases`;
const GITHUB_TOKEN = "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";
const OUTBOUND_TIMEOUT_MS = 30_000;

const { version } = JSON.parse(readFileSync(new URL("../package.json", import.meta.url), "utf8"));
const tag = `v${version}`;
const changelog = readFileSync(new URL("../CHANGELOG.md", import.meta.url), "utf8");
const section = changelog.split(/^## /m).find((part) => part.startsWith(`[${version}]`)) ?? "";

execFileSync("git", ["tag", "-a", tag, "-m", `media-intake ${tag}`], { stdio: "inherit" });
execFileSync("git", ["push", "origin", tag], { stdio: "inherit" });

const response = await fetch(API, {
  method: "POST",
  signal: AbortSignal.timeout(OUTBOUND_TIMEOUT_MS),
  headers: {
    Accept: "application/vnd.github+json",
    Authorization: `Bearer ${GITHUB_TOKEN}`,
    "X-GitHub-Api-Version": "2022-11-28",
  },
  body: JSON.stringify({ tag_name: tag, name: tag, body: section.split("\n").slice(1).join("\n").trim() }),
});
if (!response.ok) {
  console.error(`release failed: HTTP ${response.status} ${await response.text()}`);
  process.exit(1);
}
console.log(`released ${tag}`);
