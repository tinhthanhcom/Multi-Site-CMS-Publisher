import { describe, expect, it, vi } from 'vitest';
import { loadConfig } from '../src/config.js';
import { GatewayError } from '../src/lib/errors.js';
import { Orchestrator } from '../src/providers/orchestrator.js';
import type { IContentProvider, ProviderCallOptions } from '../src/providers/types.js';
import type { ProviderResult } from '../src/types.js';

const baseEnv = { AI_GATEWAY_API_KEY: 'k', PROVIDER_ORDER: 'codex,claude,gemini', MAX_CONCURRENCY: '2' };

function fakeProvider(name: string, opts: {
  available?: boolean;
  fail?: boolean;
  impl?: (o: ProviderCallOptions) => ProviderResult;
}): IContentProvider {
  return {
    name,
    isAvailable: () => opts.available ?? true,
    complete: vi.fn(async (o: ProviderCallOptions): Promise<ProviderResult> => {
      if (opts.fail) throw new Error(`${name} boom`);
      if (opts.impl) return opts.impl(o);
      return {
        article: { language: o.language, title: `${name}:${o.language}`, content: 'c', excerpt: 'e' },
        usage: { inputTokens: 10, outputTokens: 5, totalTokens: 15 },
      };
    }),
  };
}

describe('Orchestrator', () => {
  it('uses the first available provider in configured order', async () => {
    const cfg = loadConfig({ ...baseEnv } as NodeJS.ProcessEnv);
    const orch = new Orchestrator([fakeProvider('codex', {}), fakeProvider('claude', {})], cfg);
    const res = await orch.generate(
      { contentType: 'article', topic: 't', keywords: [], length: 800, tone: 'x', language: 'vi', translateTo: [] },
      'req1',
    );
    expect(res.provider).toBe('codex');
    expect(res.original.title).toBe('codex:vi');
    expect(res.usage.totalTokens).toBe(15);
  });

  it('falls back to the next provider when the first fails', async () => {
    const cfg = loadConfig({ ...baseEnv } as NodeJS.ProcessEnv);
    const orch = new Orchestrator([fakeProvider('codex', { fail: true }), fakeProvider('claude', {})], cfg);
    const res = await orch.generate(
      { contentType: 'article', topic: 't', keywords: [], length: 800, tone: 'x', language: 'vi', translateTo: [] },
      'req2',
    );
    expect(res.provider).toBe('claude');
  });

  it('skips unavailable providers', async () => {
    const cfg = loadConfig({ ...baseEnv } as NodeJS.ProcessEnv);
    const orch = new Orchestrator(
      [fakeProvider('codex', { available: false }), fakeProvider('claude', {})],
      cfg,
    );
    expect(orch.availableChain().map((p) => p.name)).toEqual(['claude']);
  });

  it('aggregates usage across original + translations', async () => {
    const cfg = loadConfig({ ...baseEnv } as NodeJS.ProcessEnv);
    const orch = new Orchestrator([fakeProvider('codex', {})], cfg);
    const res = await orch.generate(
      { contentType: 'article', topic: 't', keywords: [], length: 800, tone: 'x', language: 'vi', translateTo: ['en', 'ja'] },
      'req3',
    );
    expect(res.translations.map((t) => t.language)).toEqual(['en', 'ja']);
    expect(res.usage.totalTokens).toBe(45); // 3 calls * 15
  });

  it('throws all_providers_failed when every provider errors', async () => {
    const cfg = loadConfig({ ...baseEnv } as NodeJS.ProcessEnv);
    const orch = new Orchestrator([fakeProvider('codex', { fail: true }), fakeProvider('claude', { fail: true })], cfg);
    await expect(
      orch.generate(
        { contentType: 'article', topic: 't', keywords: [], length: 800, tone: 'x', language: 'vi', translateTo: [] },
        'req4',
      ),
    ).rejects.toBeInstanceOf(GatewayError);
  });

  it('throws no_provider_available when chain is empty', async () => {
    const cfg = loadConfig({ ...baseEnv } as NodeJS.ProcessEnv);
    const orch = new Orchestrator([fakeProvider('codex', { available: false })], cfg);
    await expect(
      orch.generate(
        { contentType: 'article', topic: 't', keywords: [], length: 800, tone: 'x', language: 'vi', translateTo: [] },
        'req5',
      ),
    ).rejects.toMatchObject({ code: 'no_provider_available' });
  });
});
