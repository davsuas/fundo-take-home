import { customerUpsertSchema } from "../schemas/customerUpsert.js";
import * as store from "../store.js";

/**
 * PUT /api/v1/customers/:id — the one write endpoint: creates a new customer or updates a
 * returning one, always answering 200 for a valid payload. GET endpoints exist so a person can
 * see what was received.
 */
export async function registerCustomersRoutes(app) {
  app.put("/customers/:id", async (request, reply) => {
    const idempotencyKey = request.headers["idempotency-key"];
    if (!idempotencyKey) {
      return reply.code(400).send({ error: "idempotency_key_required" });
    }

    const parsed = customerUpsertSchema.safeParse(request.body);
    if (!parsed.success) {
      return reply.code(400).send({ error: "validation_error", issues: parsed.error.issues });
    }

    if (parsed.data.customer.id !== request.params.id) {
      return reply.code(400).send({ error: "id_mismatch" });
    }

    const receivedAt = new Date().toISOString();
    const result = store.upsert(request.params.id, { ...parsed.data, receivedAt });

    request.log.info(
      { customerId: request.params.id, operation: parsed.data.operation, result, idempotencyKey },
      "customer upsert received",
    );

    return reply.code(200).send({ customerId: request.params.id, result, receivedAt });
  });

  app.get("/customers", async () => ({ customers: store.list() }));

  app.get("/customers/:id", async (request, reply) => {
    const record = store.get(request.params.id);
    return record ? reply.send(record) : reply.code(404).send({ error: "not_found" });
  });
}
