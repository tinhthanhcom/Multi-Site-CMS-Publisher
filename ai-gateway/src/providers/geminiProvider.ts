import { GoogleGenerativeAI } from '@google/generative-ai';
import type { Config } from '../config.js';
import type { ProviderResult } from '../types.js';
import { extractArticleJson } from '../lib/articleJson.js';
import { GatewayError } from '../lib/errors.js';
import type { IContentProvider, ProviderCallOptions } from './types.js';

const FALLBACK_MODEL = 'gemini-2.0-flash';

/** Fallback provider 2: Google Gemini. */
export class GeminiProvider implements IContentProvider {
  readonly name = 'gemini';
  private client: GoogleGenerativeAI | null;

  constructor(private readonly cfg: Config) {
    this.client = cfg.GEMINI_API_KEY ? new GoogleGenerativeAI(cfg.GEMINI_API_KEY) : null;
  }

  isAvailable(): boolean {
    return this.client !== null;
  }

  async complete(opts: ProviderCallOptions): Promise<ProviderResult> {
    if (!this.client) throw new GatewayError('gemini_unavailable', 'GEMINI_API_KEY not set', 503, this.name);

    const modelName = opts.model.startsWith('gemini') ? opts.model : FALLBACK_MODEL;
    const model = this.client.getGenerativeModel({
      model: modelName,
      systemInstruction: opts.systemPrompt,
      generationConfig: { responseMimeType: 'application/json' },
    });

    const res = await model.generateContent(opts.userPrompt);
    const text = res.response.text();
    const meta = res.response.usageMetadata;

    return {
      article: extractArticleJson(text, opts.language),
      usage: {
        inputTokens: meta?.promptTokenCount ?? 0,
        outputTokens: meta?.candidatesTokenCount ?? 0,
        totalTokens: meta?.totalTokenCount ?? 0,
      },
    };
  }
}
