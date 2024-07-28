import { z } from "zod";

const ProblemDetailErrorsSchema = z.record(z.string(), z.array(z.string()));
export type ProblemDetailErrors = z.infer<typeof ProblemDetailErrorsSchema>;

export const ProblemDetailsSchema = z.object({
  title: z.string(),
  type: z.string(),
  status: z.number(),
  message: z.string().optional(),
  errors: ProblemDetailErrorsSchema
});

export type ProblemDetails = z.infer<typeof ProblemDetailsSchema>;
