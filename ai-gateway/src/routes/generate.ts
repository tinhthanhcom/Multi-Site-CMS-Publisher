import { randomUUID } from 'node:crypto';
import type { FastifyInstance } from 'fastify';
import { z } from 'zod';
import type { Config } from '../config.js';
import { GatewayError } from '../lib/errors.js';
import { bearerAuth } from '../plugins/auth.js';
import type { Orchestrator } from '../providers/orchestrator.js';
import type { GenerateRequest } from '../types.js';

export const GenerateBody = z
  .object({
    contentType: z.enum(['article', 'product', 'news', 'social']).default('article'),
    topic: z.string().trim().min(1).optional(),
    keywords: z.array(z.string()).default([]),
    length: z.number().int().positive().max(20_000).default(800),
    tone: z.string().default('seo-friendly'),
    language: z.string().min(2).default('vi'),
    systemPrompt: z.string().optional(),
    userPrompt: z.string().optional(),
    translateTo: z.array(z.string().min(2)).default([]),
    model: z.string().optional(),
  })
  .refine((b) => Boolean(b.topic) || Boolean(b.userPrompt?.trim()), {
    message: 'Either "topic" or "userPrompt" is required',
  });

export function registerGenerateRoutes(app: FastifyInstance, cfg: Config, orchestrator: Orchestrator): void {
  app.post('/v1/generate', { preHandler: bearerAuth(cfg) }, async (req, reply) => {
    const parsed = GenerateBody.safeParse(req.body);
    if (!parsed.success) {
      throw new GatewayError('invalid_request', parsed.error.issues.map((i) => i.message).join('; '), 400);
    }

    const requestId = randomUUID();
    const body = parsed.data as GenerateRequest;

    req.log.info({ requestId, contentType: body.contentType, translateTo: body.translateTo }, 'generate:start');
    const result = await orchestrator.generate(body, requestId);
    req.log.info(
      { requestId, provider: result.provider, durationMs: result.durationMs, totalTokens: result.usage.totalTokens },
      'generate:done',
    );

    return reply.send(result);
  });
}
