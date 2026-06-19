import { timingSafeEqual } from 'node:crypto';
import type { FastifyReply, FastifyRequest } from 'fastify';
import type { Config } from '../config.js';
import { GatewayError } from '../lib/errors.js';

function safeEqual(a: string, b: string): boolean {
  const ab = Buffer.from(a);
  const bb = Buffer.from(b);
  if (ab.length !== bb.length) return false;
  return timingSafeEqual(ab, bb);
}

/**
 * Returns a preHandler that enforces `Authorization: Bearer <AI_GATEWAY_API_KEY>`.
 * Uses a constant-time comparison to avoid leaking the key via timing.
 */
export function bearerAuth(cfg: Config) {
  return async (req: FastifyRequest, _reply: FastifyReply): Promise<void> => {
    const header = req.headers['authorization'];
    if (!header || !header.startsWith('Bearer ')) {
      throw new GatewayError('unauthorized', 'Missing bearer token', 401);
    }
    const token = header.slice('Bearer '.length).trim();
    if (!safeEqual(token, cfg.AI_GATEWAY_API_KEY)) {
      throw new GatewayError('unauthorized', 'Invalid API key', 401);
    }
  };
}
