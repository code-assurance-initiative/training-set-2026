#!/usr/bin/env bash
# Materialise training-set units as freestanding git clones from their bundles.
#
#   tools/materialize.sh <unit|--all> [dest] [--tag vX.Y.Z]
#
#   <unit>    a unit name from registry.json (e.g. bench-csharp-security-secrets), or --all for every unit
#   dest      parent directory; each unit lands in <dest>/<unit> (scanners name their output after that directory,
#             so the directory is always named exactly after the unit). Default: ../training-set-2026-units,
#             next to this repository and outside it.
#   --tag     check out this tag instead of the unit's latest registered tag (one unit only)
#
# Each clone carries the unit's full history and every tag (history-only secrets and scripted sprint histories are
# part of what is measured), with `origin` removed so nothing can be pushed by accident. The script refuses a
# destination inside any git work tree: a unit nested in another repository is scanned as part of that repository
# (and its history becomes the parent's). Prints the path of every materialised unit.
set -euo pipefail

SET_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REGISTRY="$SET_ROOT/registry.json"

usage() { sed -n '2,15p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//' >&2; exit 2; }

unit="" dest="" tag=""
while [ $# -gt 0 ]; do
  case "$1" in
    --tag) [ $# -ge 2 ] || usage; tag="$2"; shift 2 ;;
    --tag=*) tag="${1#--tag=}"; shift ;;
    -h|--help) usage ;;
    *) if [ -z "$unit" ]; then unit="$1"; elif [ -z "$dest" ]; then dest="$1"; else usage; fi; shift ;;
  esac
done
[ -n "$unit" ] || usage
[ -n "$dest" ] || dest="$(dirname "$SET_ROOT")/$(basename "$SET_ROOT")-units"

# name<TAB>latestTag<TAB>bundle<TAB>bundleSha256 for every unit
units_tsv() {
  python3 - "$REGISTRY" <<'PY'
import json, sys
for u in json.load(open(sys.argv[1], encoding="utf-8"))["units"]:
    print("\t".join((u["name"], u["latestTag"], u["bundle"], u["bundleSha256"])))
PY
}

if [ "$unit" = "--all" ]; then
  [ -z "$tag" ] || { echo "materialize: --tag needs a single unit" >&2; exit 2; }
  selected="$(units_tsv)"
else
  selected="$(units_tsv | awk -F'\t' -v u="$unit" '$1 == u')"
  [ -n "$selected" ] || { echo "materialize: unknown unit '$unit' (see registry.json)" >&2; exit 2; }
fi

mkdir -p "$dest"
dest="$(cd "$dest" && pwd)"
if git -C "$dest" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "materialize: refusing: $dest is inside the git work tree $(git -C "$dest" rev-parse --show-toplevel)" >&2
  echo "materialize: choose a destination outside any repository (default: next to the set repository)" >&2
  exit 1
fi

while IFS=$'\t' read -r name latest bundle sha; do
  want="${tag:-$latest}"
  path="$SET_ROOT/$bundle"
  got="$(sha256sum "$path" | cut -d' ' -f1)"
  [ "$got" = "$sha" ] || { echo "materialize: $bundle sha256 $got != registry $sha" >&2; exit 1; }
  out="$dest/$name"
  if [ -e "$out" ]; then
    echo "materialize: $out already exists; remove it first (never overwritten)" >&2
    exit 1
  fi
  git clone -q --no-checkout "$path" "$out"
  # every branch and tag of the bundle as a local ref
  git -C "$out" fetch -q --update-head-ok "$path" '+refs/heads/*:refs/heads/*' '+refs/tags/*:refs/tags/*'
  git -C "$out" remote remove origin
  git -C "$out" rev-parse -q --verify "refs/tags/$want" >/dev/null \
    || { echo "materialize: $name has no tag $want (tags: $(git -C "$out" tag | tr '\n' ' '))" >&2; exit 1; }
  # the branch when the bundle's default branch is AT the tag (the authoring clone's state: scanners that read the
  # branch name see the same thing), otherwise a detached HEAD at the tag
  target="$(git -C "$out" rev-parse "refs/tags/$want^{commit}")"
  if [ "$(git -C "$out" rev-parse -q --verify refs/heads/main || true)" = "$target" ]; then
    git -C "$out" checkout -q main
  else
    git -C "$out" -c advice.detachedHead=false checkout -q "refs/tags/$want"
  fi
  echo "$out"
done <<< "$selected"
