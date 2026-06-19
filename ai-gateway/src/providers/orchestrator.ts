import pLimit from 'p-limit';
import type { Config } from '../config.js';
import { GatewayError } from '../lib/errors.js';
import {
  buildGenerationPrompt,
  buildSystemPrompt,
  buildTranslationPrompt,
  buildTranslationSystemPrompt,
} from '../prompt/builder.js';
import { addUsage, emptyUsage, type GenerateRequest, type GenerateResult } from '../types.js';
import type { IContentProvider, ProviderCallOptions } from './types.js';

interface FallbackOutcome<T> {
  provider: string;
  value: T;
}

export class Orchestrator {
  constructor(
    private readonly providers: IContentProvider[],
    private readonly cfg: Config,
  ) {}

  /** Providers in configured order, restricted to those currently available. */
  availableChain(): IContentProvider[] {
    const byName = new Map(this.providers.map((p) => [p.name, p]));
    return this.cfg.providerOrder
      .map((name) => byName.get(name))
      .filter((p): p is IContentProvider => Boolean(p) && p!.isAvailable());
  }

  async generate(req: GenerateRequest, requestId: string): Promise<GenerateResult> {
    const chain = this.availableChain();
    if (chain.length === 0) {
      throw new GatewayError('no_provider_available', 'No AI provider is configured/available', 503);
    }

    const model = req.model ?? this.cfg.DEFAULT_MODEL;
    const startedAt = Date.now();

    // 1) Generate the original article (with provider fallback).
    const genSystem = buildSystemPrompt(req);
    const genUser = buildGenerationPrompt(req);
    const original = await this.withFallback(chain, (p) =>
      p.complete(this.callOpts(genSystem, genUser, req.language, model)),
    );

    let usage = original.value.usage;

    // 2) Translate into each requested language (parallel, concurrency-capped).
    const limit = pLimit(this.cfg.MAX_CONCURRENCY);
    const tSystem = buildTranslationSystemPrompt();
    const translations = await Promise.all(
      req.translateTo.map((lang) =>
        limit(async () => {
          const tUser = buildTranslationPrompt(original.value.article, lang);
          const out = await this.withFallback(chain, (p) =>
            p.complete(this.callOpts(tSystem, tUser, lang, model)),
          );
          usage = addUsage(usage, out.value.usage);
          return out.value.article;
        }),
      ),
    );

    return {
      provider: original.provider,
      model,
      original: original.value.article,
      translations,
      usage,
      durationMs: Date.now() - startedAt,
      requestId,
    };
  }

  private callOpts(systemPrompt: string, userPrompt: string, language: string, model: string): ProviderCallOptions {
    return { systemPrompt, userPrompt, language, model, timeoutMs: this.cfg.REQUEST_TIMEOUT_MS };
  }

  private async withFallback<T>(
    chain: IContentProvider[],
    fn: (p: IContentProvider) => Promise<T>,
  ): Promise<FallbackOutcome<T>> {
    const errors: string[] = [];
    for (const provider of chain) {
      try {
        return { provider: provider.name, value: await fn(provider) };
      } catch (err) {
        errors.push(`${provider.name}: ${err instanceof Error ? err.message : String(err)}`);
      }
    }
    throw new GatewayError('all_providers_failed', `All providers failed → ${errors.join(' | ')}`, 502);
  }
}

export { emptyUsage };
