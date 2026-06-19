import { afterAll, beforeAll, describe, expect, it, vi } from 'vitest';
import type { FastifyInstance } from 'fastify';
import { buildApp } from '../src/app.js';
import { loadConfig } from '../src/config.js';
import type { IContentProvider, ProviderCallOptions } from '../src/providers/types.js';
import type { ProviderResult } from '../src/types.js';

const stub: IContentProvider = {
  name: 'codex',
  isAvailable: () => true,
  complete: vi.fn(async (o: ProviderCallOptions): Promise<ProviderResult> => ({
    article: { language: o.language, title: 'Stubbed', content: '<p>hi</p>', excerpt: 'e' },
    usage: { inputTokens: 1, outputTokens: 1, totalTokens: 2 },
  })),
};

let app: FastifyInstance;

beforeAll(async () => {
  const cfg = loadConfig({ AI_GATEWAY_API_KEY: 'secret', PROVIDER_ORDER: 'codex', LOG_LEVEL: 'silent' } as NodeJS.ProcessEnv);
  app = await buildApp(cfg, [stub]);
  await app.ready();
});

afterAll(async () => {
  await app.close();
});

describe('app routes', () => {
  it('GET /healthz needs no auth', async () => {
    const res = await app.inject({ method: 'GET', url: '/healthz' });
    expect(res.statusCode).toBe(200);
    expect(res.json()).toEqual({ status: 'ok' });
  });

  it('rejects /v1/generate without a bearer token (401)', async () => {
    const res = await app.inject({ method: 'POST', url: '/v1/generate', payload: { topic: 'x' } });
    expect(res.statusCode).toBe(401);
    expect(res.json().error.code).toBe('unauthorized');
  });

  it('rejects an invalid request body (400)', async () => {
    const res = await app.inject({
      method: 'POST',
      url: '/v1/generate',
      headers: { authorization: 'Bearer secret' },
      payload: { keywords: ['a'] }, // no topic / userPrompt
    });
    expect(res.statusCode).toBe(400);
    expect(res.json().error.code).toBe('invalid_request');
  });

  it('generates with a valid token + body', async () => {
    const res = await app.inject({
      method: 'POST',
      url: '/v1/generate',
      headers: { authorization: 'Bearer secret' },
      payload: { topic: 'cà phê Việt Nam', translateTo: ['en'] },
    });
    expect(res.statusCode).toBe(200);
    const body = res.json();
    expect(body.provider).toBe('codex');
    expect(body.original.title).toBe('Stubbed');
    expect(body.translations).toHaveLength(1);
    expect(body.translations[0].language).toBe('en');
    expect(body.usage.totalTokens).toBe(4); // original + 1 translation
  });
});
