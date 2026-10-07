#!/usr/bin/env bash
# Rebuilds the scripted history of this benchmark repository, deterministically.
#
# The history of ClinicScheduling is part of the benchmark's input: churn, ownership, knowledge freshness and change
# coupling are read from it. It is therefore scripted. The 120 commits from 2026-01-05 to 2026-09-30 are stored as
# patches in ./patches (NNNN.patch, one per commit, in order, as written by `git format-patch -k`), each carrying its
# fictional author and date. This script applies them on top of the key-first commit, with the committer set to the
# author and the committer date to the author date, and re-creates the release tags v0.1.0 ... v0.6.0. Because every
# input of a commit object is fixed, the result is byte-identical to the published history: the script checks the
# final commit id and every tag target against the values below.
#
# The authors are fictional (neutral invented names, addresses under the reserved .invalid domain). Commits after
# 2026-09-30 in the published history (this script, the answer key and the journal) are real benchmark maintenance
# and are not part of the scripted history.
#
# Usage: benchmark/history/build-history.sh <new-directory>
#   Run from any full (non-shallow) clone of this repository; <new-directory> must not exist. The rebuilt history is
#   left on branch `scripted` in <new-directory>.

set -euo pipefail

readonly BASE=a8b3b2a09a948d6f1b3b0ab5e5c39e846cf05218          # benchmark: answer key v1.0.0 draft (key first)
readonly EXPECTED_HEAD=431370ee0e408b03348655198f9b7a1bb9e55d13
# tag, number of scripted commits it sits on, expected commit id
readonly TAGS=(
    "v0.1.0 37 83d2d0aa53dd78ac97ac577ad5cada493dc3442f"
    "v0.2.0 54 fdfa784264fa5888813516f6098581d64244b1e8"
    "v0.3.0 66 62c0b9098c20a7e8f14bd708db7f24d1500888fe"
    "v0.4.0 85 d9d0d85a0506533383de34382e82ba74e634c9a3"
    "v0.5.0 99 68096e05d2b8f13f6c8cfe9fc59f2dcc70cf56cd"
    "v0.6.0 119 8953a11c06bdbc318f0b63b48c717e0683e140f2"
)

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source_repo="$(git -C "$here" rev-parse --show-toplevel)"
target="${1:?usage: build-history.sh <new-directory>}"
if [[ -e "$target" ]]; then
    echo "error: $target already exists" >&2
    exit 1
fi

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
    GIT_COMMITTER_NAME="$name" GIT_COMMITTER_EMAIL="$email" GIT_COMMITTER_DATE="$date" \
        git -c commit.gpgsign=false -c core.autocrlf=false \
        am --quiet --keep --keep-cr --whitespace=nowarn --committer-date-is-author-date "$patch"
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
