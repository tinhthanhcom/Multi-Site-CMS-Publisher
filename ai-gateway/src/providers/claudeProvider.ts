import Anthropic from '@anthropic-ai/sdk';
import type { Config } from '../config.js';
import type { ProviderResult } from '../types.js';
import { extractArticleJson } from '../lib/articleJson.js';
import { GatewayError } from '../lib/errors.js';
import type { IContentProvider, ProviderCallOptions } from './types.js';

const FALLBACK_MODEL = 'claude-sonnet-4-6';

/** Fallback provider 1: Anthropic Claude. */
export class ClaudeProvider implements IContentProvider {
  readonly name = 'claude';
  private client: Anthropic | null;

  constructor(private readonly cfg: Config) {
    this.client = cfg.ANTHROPIC_API_KEY ? new Anthropic({ apiKey: cfg.ANTHROPIC_API_KEY }) : null;
  }

  isAvailable(): boolean {
    return this.client !== null;
  }

  async complete(opts: ProviderCallOptions): Promise<ProviderResult> {
    if (!this.client) throw new GatewayError('claude_unavailable', 'ANTHROPIC_API_KEY not set', 503, this.name);

    // codex/gpt model ids won't resolve on Anthropic; use a Claude model.
    const model = opts.model.startsWith('claude') ? opts.model : FALLBACK_MODEL;

    const res = await this.client.messages.create({
      model,
      max_tokens: 4096,
      system: opts.systemPrompt,
      messages: [{ role: 'user', content: opts.userPrompt }],
    });

    const text = res.content
      .filter((b): b is Anthropic.TextBlock => b.type === 'text')
      .map((b) => b.text)
      .join('');

    return {
      article: extractArticleJson(text, opts.language),
      usage: {
        inputTokens: res.usage.input_tokens,
        outputTokens: res.usage.output_tokens,
        totalTokens: res.usage.input_tokens + res.usage.output_tokens,
      },
    };
  }
}
