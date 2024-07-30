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
  type: z.string(),
  title: z.string(),
  status: z.number(),
  message: z.string().optional(),
  Errors: FluentValidationErrorsScheme
});

type ProblemDetailsFluentValidation = z.infer<typeof ProblemDetailsFluentValidationSchema>;

const ProblemDetailsDetailsVariantSchema = z.object({
  type: z.string(),
  title: z.string(),
  status: z.number(),
  detail: z.string()
});

type ProblemDetailsDetailsVariant = z.infer<typeof ProblemDetailsDetailsVariantSchema>;

/**
 * Don't use this function directly. Use `clientMethodWithProblemDetails` instead.
 *
 * @deprecated Use `clientMethodWithProblemDetails` instead. This method is only used internally and will be removed in the future.
 */
export function parseServerErrorResponse(
  error: unknown | ProblemDetails | ProblemDetailsFluentValidation | ProblemDetailsDetailsVariant
): ProblemDetails | null {
  // If the error is already in the ProblemDetails format, return it as is
  const problemDetails = ProblemDetailsSchema.safeParse(error);
  if (problemDetails.success) return problemDetails.data;

  // If the error is in the ProblemDetailsDetailsVariant format, convert it to the ProblemDetails format
  const problemDetailsDetailsVariant = ProblemDetailsDetailsVariantSchema.safeParse(error);
  if (problemDetailsDetailsVariant.success) {
    console.warn("The server returned an error in the ProblemDetailsDetailsVariant format. This is not expected.");
    const { detail, ...rest } = problemDetailsDetailsVariant.data;
    return {
      ...rest,
      message: detail,
      errors: {}
    };
  }

  // If the error is in the ProblemDetailsFluentValidation format, convert it to the ProblemDetails format
  const problemDetailsFluentValidation = ProblemDetailsFluentValidationSchema.safeParse(error);
  if (problemDetailsFluentValidation.success) {
    console.warn("The server returned an error in the ProblemDetailsFluentValidation format. This is not expected.");
    const { Errors, ...rest } = problemDetailsFluentValidation.data;
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
