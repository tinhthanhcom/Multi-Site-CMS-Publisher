import { spawn } from 'node:child_process';
import { GatewayError } from './errors.js';

export interface RunOptions {
  env?: NodeJS.ProcessEnv;
  timeoutMs: number;
  cwd?: string;
}

export interface RunResult {
  code: number | null;
  stdout: string;
  stderr: string;
}

/**
 * Spawn a child process, capture stdout/stderr, and enforce a hard timeout.
 * No shell is used (args passed as an array) to avoid injection via the prompt.
 */
export function runProcess(bin: string, args: string[], opts: RunOptions): Promise<RunResult> {
  return new Promise((resolve, reject) => {
    const child = spawn(bin, args, { env: opts.env, cwd: opts.cwd, shell: false });

    let stdout = '';
    let stderr = '';
    let settled = false;

    const timer = setTimeout(() => {
      if (settled) return;
      settled = true;
      child.kill('SIGKILL');
      reject(new GatewayError('provider_timeout', `Process '${bin}' timed out after ${opts.timeoutMs}ms`, 504));
    }, opts.timeoutMs);

    child.stdout.on('data', (d: Buffer) => { stdout += d.toString(); });
    child.stderr.on('data', (d: Buffer) => { stderr += d.toString(); });

    child.on('error', (err) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      reject(new GatewayError('provider_spawn_failed', `Failed to start '${bin}': ${err.message}`, 502));
    });

    child.on('close', (code) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      resolve({ code, stdout, stderr });
    });
  });
}
