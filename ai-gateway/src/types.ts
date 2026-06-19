export type ContentType = 'article' | 'product' | 'news' | 'social';

export interface GenerateRequest {
  contentType: ContentType;
  topic?: string;
  keywords: string[];
  length: number;
  tone: string;
  language: string;
  systemPrompt?: string;
  userPrompt?: string;
  translateTo: string[];
  model?: string;
}

export interface Article {
  language: string;
  title: string;
  content: string;
  excerpt: string;
}

export interface Usage {
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
}

export interface ProviderResult {
  article: Article;
  usage: Usage;
}

export interface GenerateResult {
  provider: string;
  model: string;
  original: Article;
  translations: Article[];
  usage: Usage;
  durationMs: number;
  requestId: string;
}

export const emptyUsage = (): Usage => ({ inputTokens: 0, outputTokens: 0, totalTokens: 0 });

export const addUsage = (a: Usage, b: Usage): Usage => ({
  inputTokens: a.inputTokens + b.inputTokens,
  outputTokens: a.outputTokens + b.outputTokens,
  totalTokens: a.totalTokens + b.totalTokens,
});
