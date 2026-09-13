// In-memory store standing in for the external system's database. It does not survive restarts —
// its only job is to show the create/update contract working end to end.
const customers = new Map();

/**
 * Upserts by customer id. Delivery is at-least-once, so a retried older event can arrive after a
 * newer one; comparing `occurredAtUtc` keeps the latest submission instead of overwriting it.
 * Returns "created", "updated" or "ignored_stale".
 */
export function upsert(id, record) {
  const existing = customers.get(id);

  if (!existing) {
    customers.set(id, record);
    return "created";
  }

  if (Date.parse(record.occurredAtUtc) < Date.parse(existing.occurredAtUtc)) {
    return "ignored_stale";
  }

  customers.set(id, record);
  return "updated";
}

export function get(id) {
  return customers.get(id);
}

export function list() {
  return [...customers.values()];
}

export function clear() {
  customers.clear();
}
