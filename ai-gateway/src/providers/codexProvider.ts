import type { Config } from '../config.js';
import type { ProviderResult } from '../types.js';
import { emptyUsage } from '../types.js';
import { extractArticleJson } from '../lib/articleJson.js';
import { GatewayError } from '../lib/errors.js';
import { parseCodexJsonl } from '../lib/jsonl.js';
import { runProcess, type RunOptions, type RunResult } from '../lib/proc.js';
import type { IContentProvider, ProviderCallOptions } from './types.js';

type Runner = (bin: string, args: string[], opts: RunOptions) => Promise<RunResult>;

/**
 * Primary provider: drives the codex CLI headless and read-only so it behaves
 * as a text generator (no shell, no file edits, no network beyond the model).
 */
export class CodexProvider implements IContentProvider {
  readonly name = 'codex';

  // Runner is injectable so unit tests can stub the subprocess.
  constructor(private readonly cfg: Config, private readonly runner: Runner = runProcess) {}

  isAvailable(): boolean {
    // Available with an API key, OR when explicitly enabled (e.g. ChatGPT login).
    return this.cfg.CODEX_ENABLED === true || Boolean(this.cfg.OPENAI_API_KEY || this.cfg.CODEX_API_KEY);
  }

  async complete(opts: ProviderCallOptions): Promise<ProviderResult> {
    // Prompt is fed via stdin (not argv) to avoid shell-quoting issues and so the
    // prompt is never exposed on the command line. `codex exec` with no prompt arg
    // reads its instructions from stdin.
    const prompt = `${opts.systemPrompt}\n\n${opts.userPrompt}`;
    // `codex exec` is non-interactive by design (no approval prompts); read-only
    // sandbox keeps it from running shell/editing files — i.e. pure text gen.
    const args = [
      'exec',
      '--json',
      '--sandbox', 'read-only',
      '--skip-git-repo-check',
      '--ephemeral',
      '--ignore-user-config',
    ];
    // Only pin a model when configured; otherwise let codex use its default.
    if (this.cfg.CODEX_MODEL) args.push('-m', this.cfg.CODEX_MODEL);

    // Inherit the parent env; only set keys/home that are actually configured
    // (never blank them out — that would break ChatGPT-login auth).
    const env: NodeJS.ProcessEnv = { ...process.env };
    if (this.cfg.CODEX_HOME) env.CODEX_HOME = this.cfg.CODEX_HOME;
    if (this.cfg.OPENAI_API_KEY) env.OPENAI_API_KEY = this.cfg.OPENAI_API_KEY;
    if (this.cfg.CODEX_API_KEY) env.CODEX_API_KEY = this.cfg.CODEX_API_KEY;

    const result = await this.runner(this.cfg.CODEX_BIN, args, {
      env,
      timeoutMs: opts.timeoutMs,
      input: prompt,
      // On Windows the CLI is a .cmd shim and must be launched via the shell.
      shell: process.platform === 'win32',
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
