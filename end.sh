#!/usr/bin/env bash
# Interview kit: end.sh
# Stages the interview artifacts and creates a single commit. Run this AFTER
# you have finished the interview.
#
# Environment overrides:
#   SKIP_COMMIT=1   skip the actual git commit (used by smoke tests)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$SCRIPT_DIR"

LOG_PATH="$REPO_ROOT/interview.log"
RULES_PATH="$REPO_ROOT/INTERVIEW_RULES.md"

STAGE=()
[ -f "$LOG_PATH" ] && STAGE+=("$LOG_PATH")
[ -f "$RULES_PATH" ] && STAGE+=("$RULES_PATH")

if [ "${#STAGE[@]}" -eq 0 ]; then
    echo "✖ Nothing to stage." >&2
    exit 1
fi

if [ "${SKIP_COMMIT:-0}" = "1" ]; then
    echo "✓ end.sh: artifacts ready (SKIP_COMMIT=1 — no commit made)."
    printf '  - %s\n' "${STAGE[@]}"
    exit 0
fi

cd "$REPO_ROOT"

if [ ! -d .git ]; then
    echo "✖ Not a git repository. Was the project cloned correctly?" >&2
    exit 1
fi

git add "${STAGE[@]}"

if [ -f "$LOG_PATH" ]; then
    COMMIT_MSG="Interview session: agent log"
else
    COMMIT_MSG="Interview session"
fi

git commit -m "$COMMIT_MSG" --no-verify

echo "✓ Committed. Push instructions:"
echo "  git push origin HEAD"
echo
echo "Share the resulting repo URL with the interviewer."
