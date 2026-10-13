#!/usr/bin/env node
/**
 * Shared hook script for interview agent logging.
 * Reads JSON from stdin, appends a normalized JSONL line to interview.log.
 *
 * Usage:
 *   echo '{"prompt":"hello"}' | node scripts/log-agent-event.mjs user-prompt-submit
 *   echo '{"tool_name":"Read","tool_input":{}}' | node scripts/log-agent-event.mjs pre-tool-use
 */

import { appendFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const event = process.argv[2];
if (!event) {
  process.exit(0);
}

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const logPath = join(repoRoot, 'interview.log');

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (chunk) => {
  input += chunk;
});
process.stdin.on('end', () => {
  try {
    const payload = input.trim() ? JSON.parse(input) : {};
    const entry = {
      event,
      timestamp: new Date().toISOString(),
      ...normalizePayload(event, payload),
    };
    appendFileSync(logPath, `${JSON.stringify(entry)}\n`, 'utf8');
  } catch {
    // Hooks must never block the agent workflow.
  }
  process.exit(0);
});

function normalizePayload(event, payload) {
  if (event === 'user-prompt-submit') {
    return {
      prompt: payload.prompt ?? payload.user_message ?? payload.message ?? '',
    };
  }

  if (event === 'pre-tool-use') {
    return {
      tool_name: payload.tool_name ?? payload.toolName ?? payload.tool ?? '',
      tool_input: payload.tool_input ?? payload.toolInput ?? payload.input ?? payload,
    };
  }

  return payload;
}
