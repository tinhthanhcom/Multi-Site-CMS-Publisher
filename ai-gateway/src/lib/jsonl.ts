import type { Usage } from '../types.js';

export interface CodexExtract {
  /** Final assistant message text (from the last agent_message item). */
  text: string | null;
  /** Token usage from turn.completed, if present. */
  usage: Usage | null;
  /** Non-null when the turn failed (turn.failed event). */
  failed: string | null;
}

/**
 * Parse the newline-delimited JSON stream emitted by `codex exec --json`.
 * Extracts the agent_message text and token usage, and detects turn failures.
 * Transient lines (e.g. "Reconnecting...") and unparseable lines are ignored.
 */
export function parseCodexJsonl(stdout: string): CodexExtract {
  let text: string | null = null;
  let usage: Usage | null = null;
  let failed: string | null = null;

  for (const line of stdout.split('\n')) {
    const trimmed = line.trim();
    if (!trimmed) continue;

    let evt: Record<string, unknown>;
    try {
      evt = JSON.parse(trimmed) as Record<string, unknown>;
    } catch {
      continue; // not JSON (stray log line) — skip
    }

    const type = evt['type'];
    if (type === 'item.completed') {
      const item = evt['item'] as { type?: string; text?: string } | undefined;
      if (item?.type === 'agent_message' && typeof item.text === 'string') {
        text = item.text; // keep last agent_message
      }
    } else if (type === 'turn.completed') {
      const u = evt['usage'] as { input_tokens?: number; output_tokens?: number } | undefined;
      if (u) {
        const inputTokens = u.input_tokens ?? 0;
        const outputTokens = u.output_tokens ?? 0;
        usage = { inputTokens, outputTokens, totalTokens: inputTokens + outputTokens };
      }
    } else if (type === 'turn.failed') {
      const err = evt['error'] as { message?: string } | undefined;
      failed = err?.message ?? 'codex turn failed';
    }
  }

  return { text, usage, failed };
}
