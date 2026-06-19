import 'dotenv/config'; // load .env before reading process.env
import { buildApp } from './app.js';
import { loadConfig } from './config.js';

async function main(): Promise<void> {
  const cfg = loadConfig();
  const app = await buildApp(cfg);

  try {
    await app.listen({ port: cfg.PORT, host: '0.0.0.0' });
    app.log.info(
      { providerOrder: cfg.providerOrder, defaultModel: cfg.DEFAULT_MODEL },
      `ai-gateway listening on :${cfg.PORT}`,
    );
  } catch (err) {
    app.log.error(err, 'failed to start');
    process.exit(1);
  }
}

void main();
