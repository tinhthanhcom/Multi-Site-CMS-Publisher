import type { Article, GenerateRequest } from '../types.js';

const JSON_CONTRACT = `Return ONLY a single JSON object with exactly these string fields:
{"title": "...", "content": "...", "excerpt": "..."}
- "content" must be HTML body markup (use <h2>, <p>, <ul>, <li>, etc.) WITHOUT <html>/<body> wrappers.
- "excerpt" is a short plain-text summary (1-2 sentences).
- Do NOT wrap the JSON in markdown code fences. Output nothing but the JSON object.`;

export function buildSystemPrompt(req: GenerateRequest): string {
  const custom = req.systemPrompt?.trim();
  if (custom) return custom;
  return `You are an expert ${req.contentType} writer. You produce original, well-structured, SEO-aware content. You always reply with a single valid JSON object and nothing else.`;
}

export function buildGenerationPrompt(req: GenerateRequest): string {
  const custom = req.userPrompt?.trim();
  const base = custom
    ? custom
    : [
        `Write a ${req.contentType} about "${req.topic}".`,
        req.keywords.length ? `Naturally incorporate these keywords: ${req.keywords.join(', ')}.` : '',
        `Target length: about ${req.length} words.`,
        `Tone: ${req.tone}.`,
        `Write it in this language (BCP-47/ISO code): ${req.language}.`,
      ]
        .filter(Boolean)
        .join(' ');

  return `${base}\n\n${JSON_CONTRACT}`;
}

export function buildTranslationSystemPrompt(): string {
  return 'You are a professional translator and localizer. You preserve meaning, tone, HTML structure, and SEO intent. You always reply with a single valid JSON object and nothing else.';
}

export function buildTranslationPrompt(article: Article, targetLanguage: string): string {
  return `Translate the following article into this language (code: ${targetLanguage}). Keep the HTML structure of the content intact.\n\n${JSON_CONTRACT}\n\nSOURCE TITLE: ${article.title}\nSOURCE EXCERPT: ${article.excerpt}\nSOURCE CONTENT:\n${article.content}`;
}
