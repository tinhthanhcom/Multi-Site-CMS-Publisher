import type { Config } from '../config.js';
import type { ProviderResult } from '../types.js';
import { emptyUsage } from '../types.js';
import { extractArticleJson } from '../lib/articleJson.js';
import { GatewayError } from '../lib/errors.js';
import { parseCodexJsonl } from '../lib/jsonl.js';
import { runProcess, type RunResult } from '../lib/proc.js';
import type { IContentProvider, ProviderCallOptions } from './types.js';

type Runner = (bin: string, args: string[], opts: { env?: NodeJS.ProcessEnv; timeoutMs: number }) => Promise<RunResult>;

/**
 * Primary provider: drives the codex CLI headless and read-only so it behaves
 * as a text generator (no shell, no file edits, no network beyond the model).
 */
export class CodexProvider implements IContentProvider {
  readonly name = 'codex';

  // Runner is injectable so unit tests can stub the subprocess.
  constructor(private readonly cfg: Config, private readonly runner: Runner = runProcess) {}

  isAvailable(): boolean {
    return Boolean(this.cfg.OPENAI_API_KEY || this.cfg.CODEX_API_KEY);
  }

  async complete(opts: ProviderCallOptions): Promise<ProviderResult> {
    const prompt = `${opts.systemPrompt}\n\n${opts.userPrompt}`;
    const args = [
      'exec',
      '--json',
      '--sandbox', 'read-only',
      '--ask-for-approval', 'never',
      '--skip-git-repo-check',
      '--ephemeral',
      '--ignore-user-config',
      '-m', opts.model,
      prompt,
    ];

    const key = this.cfg.OPENAI_API_KEY ?? this.cfg.CODEX_API_KEY ?? '';
    const result = await this.runner(this.cfg.CODEX_BIN, args, {
      env: { ...process.env, OPENAI_API_KEY: key, CODEX_API_KEY: this.cfg.CODEX_API_KEY ?? key },
      timeoutMs: opts.timeoutMs,
    });

    const extract = parseCodexJsonl(result.stdout);
    if (extract.failed) {
      throw new GatewayError('codex_turn_failed', `codex: ${extract.failed}`, 502, this.name);
    }
    if (!extract.text) {
      const tail = result.stderr.trim().slice(-300);
      throw new GatewayError('codex_no_output', `codex produced no agent message${tail ? ` (${tail})` : ''}`, 502, this.name);
    }

    return {
      article: extractArticleJson(extract.text, opts.language),
      usage: extract.usage ?? emptyUsage(),
    };
  }
}
