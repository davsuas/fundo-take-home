import { z } from "zod";

const addressSchema = z.object({
  street: z.string().min(1).max(200),
  city: z.string().min(1).max(100),
  state: z.string().length(2),
  postalCode: z.string().regex(/^\d{5}$/, "postalCode must be exactly 5 digits"),
});

/** The contract the backend's HttpExternalCustomerRegistry sends (CustomerUpsertPayload in C#). */
export const customerUpsertSchema = z.object({
  customer: z.object({
    id: z.uuid(),
    firstName: z.string().min(1),
    lastName: z.string().min(1),
    companyName: z.string().min(1),
    ssnLast4: z.string().regex(/^\d{4}$/, "ssnLast4 must be exactly 4 digits"),
    address: addressSchema,
  }),
  application: z.object({
    id: z.uuid(),
    requestedAmount: z.number().positive(),
    currency: z.literal("USD"),
    status: z.string().min(1),
  }),
  operation: z.enum(["create", "update"]),
  occurredAtUtc: z.iso.datetime(),
});
