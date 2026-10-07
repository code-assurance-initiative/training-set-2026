#!/usr/bin/env bash
# Verify this training set against its registry.json. Exit 0 only when every check of every unit passes.
#
#   tools/verify.sh            all units
#   tools/verify.sh <unit>...  only these units
#
# Per unit:
#   1. sha256 of units/<unit>.bundle equals registry bundleSha256, and `git bundle verify` accepts it;
#   2. every registered version's tag resolves (peeled) to its registered commit, and benchmark/answer-key.json at
#      that tag has the registered keySha256 (the latest version is the unit's latestTag/commit/keySha256);
#   3. the readable snapshot units/<unit>/ is byte-for-byte the tree of the latest tag (same git tree id).
set -euo pipefail

SET_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REGISTRY="$SET_ROOT/registry.json"
TMP="$(mktemp -d "${TMPDIR:-/tmp}/training-set-verify.XXXXXX")"
trap 'rm -rf "$TMP"' EXIT

# name<TAB>latestTag<TAB>commit<TAB>keySha256<TAB>bundle<TAB>bundleSha256<TAB>snapshot<TAB>tag=commit=key,...
rows="$(python3 - "$REGISTRY" "$@" <<'PY'
import json, sys
reg = json.load(open(sys.argv[1], encoding="utf-8"))
want = set(sys.argv[2:])
units = reg["units"]
unknown = want - {u["name"] for u in units}
if unknown:
    sys.exit(f"verify: unknown unit(s): {' '.join(sorted(unknown))}")
for u in units:
    if want and u["name"] not in want:
        continue
    last = u["versions"][-1]
    if (last["tag"], last["commit"], last["keySha256"]) != (u["latestTag"], u["commit"], u["keySha256"]):
        sys.exit(f"verify: {u['name']}: latestTag/commit/keySha256 is not its last registered version")
    vs = ",".join(f"{v['tag']}={v['commit']}={v['keySha256']}" for v in u["versions"])
    print("\t".join((u["name"], u["latestTag"], u["commit"], u["keySha256"], u["bundle"], u["bundleSha256"],
                     u["snapshot"], vs)))
PY
)"

fail=0 n=0
while IFS=$'\t' read -r name latest commit keysha bundle bsha snapshot versions; do
  n=$((n + 1))
  ufail=0
  bad() { echo "FAIL $name: $*"; fail=1; ufail=1; }
  path="$SET_ROOT/$bundle"
  if [ ! -f "$path" ]; then bad "$bundle missing"; continue; fi
  got="$(sha256sum "$path" | cut -d' ' -f1)"
  [ "$got" = "$bsha" ] || bad "bundle sha256 $got != registry $bsha"
  git bundle verify -q "$path" >/dev/null 2>&1 || bad "git bundle verify rejects $bundle"
  repo="$TMP/$name.git"
  git init -q --bare "$repo"
  git -C "$repo" fetch -q "$path" '+refs/*:refs/*'
  IFS=',' read -ra vs <<< "$versions"
  for v in "${vs[@]}"; do
    IFS='=' read -r tag vcommit vkey <<< "$v"
    c="$(git -C "$repo" rev-parse -q --verify "refs/tags/$tag^{commit}" || true)"
    if [ "$c" != "$vcommit" ]; then bad "$tag -> ${c:-missing}, registry $vcommit"; continue; fi
    k="$(git -C "$repo" show "$tag:benchmark/answer-key.json" | sha256sum | cut -d' ' -f1)"
    [ "$k" = "$vkey" ] || bad "$tag key sha256 $k != registry $vkey"
  done
  snap="$SET_ROOT/$snapshot"
  if [ -d "$snap" ]; then
    idx="$TMP/$name.index"
    GIT_INDEX_FILE="$idx" git --git-dir="$repo" --work-tree="$snap" -c core.autocrlf=false add -A -f . 2>/dev/null
    st="$(GIT_INDEX_FILE="$idx" git --git-dir="$repo" write-tree)"
    tt="$(git -C "$repo" rev-parse "refs/tags/$latest^{tree}")"
    [ "$st" = "$tt" ] || bad "snapshot $snapshot tree $st != $latest tree $tt"
  else
    bad "snapshot $snapshot missing"
  fi
  [ "$ufail" = 1 ] || echo "ok   $name ($latest, ${#vs[@]} version(s))"
done <<< "$rows"

if [ "$fail" = 0 ]; then echo "verify: $n unit(s) OK"; else echo "verify: FAILED"; exit 1; fi
