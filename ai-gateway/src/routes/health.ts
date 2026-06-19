import type { FastifyInstance } from 'fastify';
import type { Config } from '../config.js';
import { bearerAuth } from '../plugins/auth.js';
import type { Orchestrator } from '../providers/orchestrator.js';

export function registerHealthRoutes(app: FastifyInstance, cfg: Config, orchestrator: Orchestrator): void {
  // Liveness — no auth (for load balancer / uptime checks).
  app.get('/healthz', async () => ({ status: 'ok' }));

  // Provider readiness — requires auth.
  app.get('/v1/providers', { preHandler: bearerAuth(cfg) }, async () => {
    const available = new Set(orchestrator.availableChain().map((p) => p.name));
    return {
      order: cfg.providerOrder,
      providers: cfg.providerOrder.map((name) => ({ name, available: available.has(name) })),
      defaultModel: cfg.DEFAULT_MODEL,
    };
  });
}
