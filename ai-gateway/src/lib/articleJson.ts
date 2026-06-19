import type { Article } from '../types.js';
import { GatewayError } from './errors.js';

/**
 * Robustly extract an {title, content, excerpt} JSON object from a model's
 * raw text output — tolerating markdown code fences and leading/trailing prose.
 */
export function extractArticleJson(raw: string, language: string): Article {
  let s = raw.trim();

  // Strip a ```json ... ``` (or ``` ... ```) fence if present.
  const fence = s.match(/```(?:json)?\s*([\s\S]*?)```/i);
  if (fence?.[1]) s = fence[1].trim();

  // Narrow to the outermost JSON object.
  const start = s.indexOf('{');
  const end = s.lastIndexOf('}');
  if (start !== -1 && end !== -1 && end > start) s = s.slice(start, end + 1);

  let obj: Record<string, unknown>;
  try {
    obj = JSON.parse(s) as Record<string, unknown>;
  } catch {
    throw new GatewayError('bad_model_output', 'Model did not return parseable JSON', 502);
  }

  const title = String(obj['title'] ?? '').trim();
  const content = String(obj['content'] ?? '').trim();
  const excerpt = String(obj['excerpt'] ?? '').trim();

  if (!title && !content) {
    throw new GatewayError('bad_model_output', 'Model JSON missing title/content', 502);
  }

  return { language, title, content, excerpt };
}
