import type { ProviderResult } from '../types.js';

export interface ProviderCallOptions {
  systemPrompt: string;
  userPrompt: string;
  /** Language code to stamp on the returned Article. */
  language: string;
  model: string;
  timeoutMs: number;
}

/**
 * A content provider performs one structured completion: given a system +
 * user prompt, it returns an {title, content, excerpt} Article plus usage.
 * Generation and translation both go through complete() with different prompts.
 */
export interface IContentProvider {
  readonly name: string;
  /** True when the required credentials/binary are configured. */
  isAvailable(): boolean;
  complete(opts: ProviderCallOptions): Promise<ProviderResult>;
}
