import { describe, expect, it } from 'vitest';
import { parseCodexJsonl } from '../src/lib/jsonl.js';

describe('parseCodexJsonl', () => {
  it('extracts agent_message text and usage from a codex JSONL stream', () => {
    const stream = [
      '{"type":"thread.started","thread_id":"t1"}',
      '{"type":"turn.started"}',
      '{"type":"item.started","item":{"id":"i1","type":"agent_message"}}',
      '{"type":"item.completed","item":{"id":"i1","type":"agent_message","text":"{\\"title\\":\\"Hi\\"}"}}',
      '{"type":"turn.completed","usage":{"input_tokens":100,"output_tokens":42}}',
    ].join('\n');

    const out = parseCodexJsonl(stream);
    expect(out.text).toBe('{"title":"Hi"}');
    expect(out.usage).toEqual({ inputTokens: 100, outputTokens: 42, totalTokens: 142 });
    expect(out.failed).toBeNull();
  });

  it('ignores non-JSON noise lines and Reconnecting notices', () => {
    const stream = [
      'Reconnecting... 1/5',
      'some stray log',
      '{"type":"item.completed","item":{"type":"agent_message","text":"ok"}}',
    ].join('\n');
    expect(parseCodexJsonl(stream).text).toBe('ok');
  });

  it('reports turn.failed as a failure', () => {
    const stream = '{"type":"turn.failed","error":{"message":"rate limited"}}';
    const out = parseCodexJsonl(stream);
    expect(out.failed).toBe('rate limited');
    expect(out.text).toBeNull();
  });

  it('keeps the last agent_message when multiple are present', () => {
    const stream = [
      '{"type":"item.completed","item":{"type":"agent_message","text":"first"}}',
      '{"type":"item.completed","item":{"type":"agent_message","text":"second"}}',
    ].join('\n');
    expect(parseCodexJsonl(stream).text).toBe('second');
  });
});
