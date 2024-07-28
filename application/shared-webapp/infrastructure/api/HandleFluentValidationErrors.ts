/**
 * This file contains workarounds for the API not returning errors in the standard ProblemDetails format.
 *
 * Use `clientMethodWithProblemDetails` instead, this file is only used internally and will be removed in the future.
 */

import { getCamelCase } from "@repo/utils/string/getCamelCase";
import { ProblemDetailsSchema, type ProblemDetailErrors, type ProblemDetails } from "./ProblemDetails";
import { z } from "zod";

const FluentValidationErrorsScheme = z.array(z.object({ code: z.string(), message: z.string() }));
type FluentValidationErrors = z.infer<typeof FluentValidationErrorsScheme>;

const ProblemDetailsFluentValidationSchema = z.object({
  title: z.string(),
  type: z.string(),
  status: z.number(),
  message: z.string().optional(),
  Errors: FluentValidationErrorsScheme
});

type ProblemDetailsFluentValidation = z.infer<typeof ProblemDetailsFluentValidationSchema>;

/**
 * Don't use this function directly. Use `clientMethodWithProblemDetails` instead.
 *
 * @deprecated Use `clientMethodWithProblemDetails` instead. This method is only used internally and will be removed in the future.
 */
export function parseServerErrorResponse(
  error: unknown | ProblemDetails | ProblemDetailsFluentValidation
): ProblemDetails | null {
  const problemDetails = ProblemDetailsSchema.safeParse(error);
  if (problemDetails.success) return problemDetails.data;

  const ProblemDetailsFluentValidation = ProblemDetailsFluentValidationSchema.safeParse(error);

  if (ProblemDetailsFluentValidation.success) {
    const { Errors, ...rest } = ProblemDetailsFluentValidation.data;
    return {
      ...rest,
      errors: convertFluentValidationErrors(Errors)
    };
  }

  return null;
}

function convertFluentValidationErrors(validationErrors: FluentValidationErrors): ProblemDetailErrors {
  const fieldErrors: ProblemDetailErrors = {};
  for (const error of validationErrors) {
    const key = getCamelCase(error.code);
    if (fieldErrors[key] == null) {
      fieldErrors[key] = [error.message];
    } else {
      fieldErrors[key].push(error.message);
    }
  }
  return fieldErrors;
}
