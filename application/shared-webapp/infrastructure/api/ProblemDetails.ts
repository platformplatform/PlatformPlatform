import { z } from "zod";

const ProblemDetailErrorsSchema = z.record(z.string(), z.array(z.string()));
export type ProblemDetailErrors = z.infer<typeof ProblemDetailErrorsSchema>;

export const ProblemDetailsSchema = z.object({
  title: z.string().optional(), // Short description of the problem
  type: z.string().optional(), // "about:blank"
  status: z.number().optional(), // Actual HTTP status code
  detail: z.string().optional(), // Guidance for the client to resolve the problem
  instance: z.string().optional(), // URI to the specific instance of the problem
  errors: ProblemDetailErrorsSchema.optional() // Additional details about specific fields that caused the problem
}); // We don't apply "strict" as the spec allows for additional properties

export type ProblemDetails = z.infer<typeof ProblemDetailsSchema>;

export class ProblemDetailsError extends Error {
  constructor(public readonly details: ProblemDetails) {
    super(details.title);
  }
}
