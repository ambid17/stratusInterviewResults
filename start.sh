#!/usr/bin/env bash
# Interview kit: start.sh
# Records the terminal session for the duration of the interview. Run this from
# the repo root BEFORE you start working, and do your work inside the shell it
# opens.
#
# Environment overrides:
#   SKIP_RECORD=1   skip the actual recording (used by smoke tests)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$SCRIPT_DIR"
STATE_DIR="$REPO_ROOT/.interview-state"
TRANSCRIPT="$REPO_ROOT/session.log"

mkdir -p "$STATE_DIR"
date -u +"%Y-%m-%dT%H:%M:%SZ" > "$STATE_DIR/started-at"

echo "Stratus Field Link — interview session"
echo "  repo:       $REPO_ROOT"
echo "  transcript: $TRANSCRIPT"
echo
echo "Read README.md for the brief. Run ./end.sh when you are done."
echo

if [ "${SKIP_RECORD:-0}" = "1" ]; then
    echo "✓ start.sh: ready (SKIP_RECORD=1 — no recording started)."
    exit 0
fi

if ! command -v script >/dev/null 2>&1; then
    echo "⚠ 'script' is not available; continuing without a terminal transcript."
    exit 0
fi

cd "$REPO_ROOT"

# BSD (macOS) and GNU (Linux) 'script' take different arguments.
if script --version >/dev/null 2>&1; then
    script -q -a -c "${SHELL:-/bin/bash}" "$TRANSCRIPT"
else
    script -q -a "$TRANSCRIPT"
fi

echo
echo "Recording stopped. Now run ./end.sh"
