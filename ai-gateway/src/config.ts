import { z } from 'zod';

const schema = z.object({
  PORT: z.coerce.number().int().positive().default(8080),
  LOG_LEVEL: z.string().default('info'),
  AI_GATEWAY_API_KEY: z.string().min(1, 'AI_GATEWAY_API_KEY is required'),
  OPENAI_API_KEY: z.string().optional(),
  CODEX_API_KEY: z.string().optional(),
  ANTHROPIC_API_KEY: z.string().optional(),
  GEMINI_API_KEY: z.string().optional(),
  DEFAULT_MODEL: z.string().default('gpt-5.4'),
  PROVIDER_ORDER: z.string().default('codex,claude,gemini'),
  MAX_CONCURRENCY: z.coerce.number().int().positive().default(3),
  REQUEST_TIMEOUT_MS: z.coerce.number().int().positive().default(120_000),
  CODEX_BIN: z.string().default('codex'),
});

export type Config = z.infer<typeof schema> & { providerOrder: string[] };

export function loadConfig(env: NodeJS.ProcessEnv = process.env): Config {
  const parsed = schema.parse(env);
  const providerOrder = parsed.PROVIDER_ORDER.split(',')
    .map((s) => s.trim())
    .filter(Boolean);
  return { ...parsed, providerOrder };
}
