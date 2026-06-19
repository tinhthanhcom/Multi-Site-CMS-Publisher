import type { Config } from '../config.js';
import type { ProviderResult } from '../types.js';
import type { IContentProvider, ProviderCallOptions } from './types.js';

/**
 * Deterministic, dependency-free provider for LOCAL end-to-end testing.
 * Enabled only when MOCK_PROVIDER=true. Produces plausible HTML article content
 * (and "translations") so the full CMS → gateway → editor flow can be exercised
 * without real API keys or the codex CLI. Never enable in production.
 */
export class MockProvider implements IContentProvider {
  readonly name = 'mock';

  constructor(private readonly cfg: Config) {}

  isAvailable(): boolean {
    return this.cfg.MOCK_PROVIDER === true;
  }

  // eslint-disable-next-line @typescript-eslint/require-await
  async complete(opts: ProviderCallOptions): Promise<ProviderResult> {
    const isTranslation = /translate/i.test(opts.userPrompt) || /translator/i.test(opts.systemPrompt);
    const topic = extractTopic(opts.userPrompt);
    const prefix = isTranslation ? `[MOCK ${opts.language}]` : '[MOCK]';

    const title = `${prefix} ${topic}`;
    const content =
      `<h2>${escapeHtml(topic)}</h2>` +
      `<p>Đây là nội dung mẫu do <strong>MockProvider</strong> sinh ra (ngôn ngữ: ${opts.language}) ` +
      `để kiểm thử luồng end-to-end mà không cần API key thật.</p>` +
      `<h3>Mục 1</h3><p>Đoạn văn minh hoạ về "${escapeHtml(topic)}".</p>` +
      `<ul><li>Điểm A</li><li>Điểm B</li><li>Điểm C</li></ul>` +
      `<h3>Kết luận</h3><p>Tóm tắt ngắn gọn cho chủ đề "${escapeHtml(topic)}".</p>`;
    const excerpt = `Bài viết mẫu (mock) về ${topic}.`;

    // Stable, non-zero usage so token tracking can be observed.
    const promptLen = opts.systemPrompt.length + opts.userPrompt.length;
    const inputTokens = Math.ceil(promptLen / 4);
    const outputTokens = Math.ceil(content.length / 4);

    return {
      article: { language: opts.language, title, content, excerpt },
      usage: { inputTokens, outputTokens, totalTokens: inputTokens + outputTokens },
    };
  }
}

function extractTopic(userPrompt: string): string {
  // Translation prompts carry the original title; reuse it.
  const srcTitle = userPrompt.match(/SOURCE TITLE:\s*(.+)/i);
  if (srcTitle?.[1]) return srcTitle[1].trim();
  // Generation prompts: about "<topic>"
  const quoted = userPrompt.match(/about\s+"([^"]+)"/i);
  if (quoted?.[1]) return quoted[1].trim();
  const firstLine = userPrompt.split('\n')[0]?.trim() ?? '';
  return (firstLine.length > 60 ? firstLine.slice(0, 60) + '…' : firstLine) || 'Chủ đề mẫu';
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}
