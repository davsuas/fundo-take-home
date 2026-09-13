import Fastify from "fastify";
import { registerCustomersRoutes } from "./routes/customers.js";
import { registerHealthRoutes } from "./routes/health.js";

/** Builds the Fastify instance without starting a listener — used by both server.js and tests. */
export function buildApp(options = {}) {
  const app = Fastify({ logger: options.logger ?? true });

  app.register(registerHealthRoutes);
  app.register(registerCustomersRoutes, { prefix: "/api/v1" });

  return app;
}
