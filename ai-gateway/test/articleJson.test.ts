import { describe, expect, it } from 'vitest';
import { extractArticleJson } from '../src/lib/articleJson.js';
import { GatewayError } from '../src/lib/errors.js';

describe('extractArticleJson', () => {
  it('parses a clean JSON object', () => {
    const a = extractArticleJson('{"title":"T","content":"<p>C</p>","excerpt":"E"}', 'vi');
    expect(a).toEqual({ language: 'vi', title: 'T', content: '<p>C</p>', excerpt: 'E' });
  });

  it('strips markdown code fences', () => {
    const raw = '```json\n{"title":"T","content":"<p>C</p>","excerpt":"E"}\n```';
    expect(extractArticleJson(raw, 'en').title).toBe('T');
  });

  it('tolerates leading/trailing prose around the object', () => {
    const raw = 'Sure! Here you go:\n{"title":"T","content":"C","excerpt":""}\nHope that helps.';
    const a = extractArticleJson(raw, 'en');
    expect(a.title).toBe('T');
    expect(a.content).toBe('C');
  });

  it('throws GatewayError on unparseable output', () => {
    expect(() => extractArticleJson('not json at all', 'vi')).toThrow(GatewayError);
  });

  it('throws when title and content are both missing', () => {
    expect(() => extractArticleJson('{"excerpt":"only"}', 'vi')).toThrow(GatewayError);
  });
});
