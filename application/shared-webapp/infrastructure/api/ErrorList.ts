import { getCamelCase } from "@repo/utils/string/getCamelCase";
import { z } from "zod";

export const ApiErrorListSchema = z.array(z.object({ code: z.string(), message: z.string() }));
export type ApiErrorList = z.infer<typeof ApiErrorListSchema>;

const ApiErrorSchema = z.object({
  title: z.string(),
  type: z.string(),
  status: z.number(),
  Errors: ApiErrorListSchema
});
type ApiError = z.infer<typeof ApiErrorSchema>;

const ApiResponseSchema = z.object({
  error: ApiErrorSchema.optional()
});
type ApiResponse = z.infer<typeof ApiResponseSchema>;

export function getApiError<R>(response: R): ApiError {
  const result = ApiResponseSchema.safeParse(response);
  if (!result.success || result.data.error == null) {
    return {
      title: "Unknown server error response",
      status: 0,
      type: "0",
      Errors: []
    };
  }
  return result.data.error;
}

export function getFieldErrors(apiErrorList: ApiErrorList): Record<string, string[]> {
  const fieldErrors: Record<string, string[]> = {};
  for (const error of apiErrorList) {
    const key = getCamelCase(error.code);
    if (fieldErrors[key] == null) fieldErrors[key] = [];

    fieldErrors[key].push(error.message);
  }
  return fieldErrors;
}
