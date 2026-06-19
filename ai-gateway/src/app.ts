import rateLimit from '@fastify/rate-limit';
import Fastify, { type FastifyInstance } from 'fastify';
import type { Config } from './config.js';
import { GatewayError } from './lib/errors.js';
import { CodexProvider } from './providers/codexProvider.js';
import { ClaudeProvider } from './providers/claudeProvider.js';
import { GeminiProvider } from './providers/geminiProvider.js';
import { Orchestrator } from './providers/orchestrator.js';
import type { IContentProvider } from './providers/types.js';
import { registerGenerateRoutes } from './routes/generate.js';
import { registerHealthRoutes } from './routes/health.js';

/**
 * Build a fully-wired Fastify instance. Exposed separately from server.ts so
 * tests can spin up the app (with injected providers) without binding a port.
 */
export async function buildApp(cfg: Config, providers?: IContentProvider[]): Promise<FastifyInstance> {
  const app = Fastify({
    logger: { level: cfg.LOG_LEVEL },
    bodyLimit: 1_048_576, // 1 MiB
  });

  await app.register(rateLimit, { max: 60, timeWindow: '1 minute' });

  const chain: IContentProvider[] =
    providers ?? [new CodexProvider(cfg), new ClaudeProvider(cfg), new GeminiProvider(cfg)];
  const orchestrator = new Orchestrator(chain, cfg);

  registerHealthRoutes(app, cfg, orchestrator);
  registerGenerateRoutes(app, cfg, orchestrator);

  // Centralized error shaping — never leak stack/secrets to the client.
  app.setErrorHandler((err, req, reply) => {
    if (err instanceof GatewayError) {
      if (err.statusCode >= 500) req.log.error({ code: err.code, provider: err.provider }, err.message);
      return reply
        .status(err.statusCode)
        .send({ error: { code: err.code, message: err.message, provider: err.provider } });
    }
    // Fastify validation / rate-limit / unknown
    const status = (err as { statusCode?: number }).statusCode ?? 500;
    const message = err instanceof Error ? err.message : 'Unexpected error';
    if (status >= 500) req.log.error({ err }, 'unhandled error');
    return reply
      .status(status)
      .send({ error: { code: status === 429 ? 'rate_limited' : 'internal_error', message } });
  });

  return app;
}
