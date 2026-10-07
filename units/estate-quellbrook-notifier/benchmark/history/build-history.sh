#!/usr/bin/env bash
# Rebuilds the scripted history of this benchmark repository, deterministically.
#
# The history is part of the benchmark's input: churn, ownership, knowledge freshness, change coupling, sprint trends
# and history-only findings are read from it. Its 25 commits (2026-07-27 .. 2026-09-04, three two-week
# sprints) are stored as patches in ./patches (NNNN.patch, one per commit, in order, as written by
# `git format-patch -k`), each carrying its fictional author and date. This script applies them on top of the
# key-first commit with the committer set to the author and the committer date to the author date, and re-creates the
# sprint release tags. Every input of a commit object is fixed, so the result is byte-identical to the published
# history: the script checks the final commit id and every tag target.
#
# The authors are fictional (neutral invented names under the reserved .invalid domain). Commits after the scripted
# history (the answer key's maintenance, this script, the journal) are real benchmark maintenance.
#
# Usage: benchmark/history/build-history.sh <new-directory>
#   Run from any full (non-shallow) clone of this repository; <new-directory> must not exist. The rebuilt history is
#   left on branch `scripted` in <new-directory>.

set -euo pipefail

readonly BASE=4971ae8d246ffd5f58f752f1a7c107f1e11d6469
readonly EXPECTED_HEAD=4d48e7b1261f1423255dc7de57faa7f983842559
# tag, number of scripted commits it sits on, expected commit id
readonly TAGS=(
    "v0.1.0 10 6d4297e4b2f8d3c58024a8a9af06dd765aad2752"
    "v0.2.0 17 a65e0044c86c7b1e55f27f81328e4a96b1f9284f"
    "v0.3.0 25 4d48e7b1261f1423255dc7de57faa7f983842559"
)

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source_repo="$(git -C "$here" rev-parse --show-toplevel)"
target="${1:?usage: build-history.sh <new-directory>}"
if [[ -e "$target" ]]; then
    echo "error: $target already exists" >&2
    exit 1
fi

# The provider key that the scripted history committed (and later removed) is not stored in plain text in this
# directory: the working tree must not carry it (it is a history-only finding). The patches carry the marker
# @@REDACTED_SECRET@@ in its place, and the rebuild takes the value from the published commit that introduced it,
# which every full clone of this repository has.
readonly SECRET_COMMIT=a033c7cf647a114975fc8d1cbcba5ab046d6427a
readonly SECRET_PATH=src/Quellbrook.Notifier/appsettings.json
secret="$(git -C "$source_repo" show "$SECRET_COMMIT:$SECRET_PATH" | grep -oE 'SG\.[A-Za-z0-9_-]{22}\.[A-Za-z0-9_-]{43}' | head -n 1)"
if [[ -z "$secret" ]]; then
    echo "error: cannot read the redacted value from $SECRET_COMMIT:$SECRET_PATH" >&2
    exit 1
fi

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
git clone --quiet --no-checkout --no-tags "$source_repo" "$target"
cd "$target"
git checkout --quiet -b scripted "$BASE"

n=0
while IFS= read -r patch; do
    n=$((n + 1))
    author="$(sed -n 's/^From: //p' "$patch" | head -n 1)"
    date="$(sed -n 's/^Date: //p' "$patch" | head -n 1)"
    name="${author% <*}"
    email="${author##*<}"
    email="${email%>}"
    input="$patch"
    if grep -q '@@REDACTED_SECRET@@' "$patch"; then
        input="$work/$(basename "$patch")"
        sed "s|@@REDACTED_SECRET@@|$secret|g" "$patch" > "$input"
    fi
    GIT_COMMITTER_NAME="$name" GIT_COMMITTER_EMAIL="$email" GIT_COMMITTER_DATE="$date" \
        git -c commit.gpgsign=false -c core.autocrlf=false \
        am --quiet --keep --keep-cr --whitespace=nowarn --committer-date-is-author-date "$input"
    for entry in "${TAGS[@]}"; do
        read -r tag at expected <<<"$entry"
        if [[ "$at" == "$n" ]]; then
            GIT_COMMITTER_NAME="$name" GIT_COMMITTER_EMAIL="$email" GIT_COMMITTER_DATE="$date" \
                git -c tag.gpgsign=false tag -f -a "$tag" -m "Release ${tag#v}" HEAD >/dev/null
            actual="$(git rev-parse "$tag^{commit}")"
            if [[ "$actual" != "$expected" ]]; then
                echo "error: $tag is $actual, expected $expected" >&2
                exit 1
            fi
        fi
    done
done < <(find "$here/patches" -name '*.patch' | LC_ALL=C sort)

head="$(git rev-parse HEAD)"
if [[ "$head" != "$EXPECTED_HEAD" ]]; then
    echo "error: rebuilt history ends at $head, expected $EXPECTED_HEAD" >&2
    exit 1
fi
echo "rebuilt $n scripted commits on top of ${BASE:0:7}; HEAD $head matches; ${#TAGS[@]} tags verified"
