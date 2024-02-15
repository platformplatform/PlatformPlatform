import type { FetchResponse } from "openapi-fetch";
import { z } from "zod";
import { getCamelCase } from "./getCamelCase.ts";

const ApiErrorListSchema = z.array(z.object({ code: z.string(), message: z.string() }));
type ApiErrorList = z.infer<typeof ApiErrorListSchema>;

const ApiErrorSchema = z.object({
  title: z.string(),
  type: z.string(),
  status: z.number(),
  Errors: ApiErrorListSchema,
});
type ApiError = z.infer<typeof ApiErrorSchema>;

export function getApiError(response: FetchResponse<any>): ApiError {
  const { error = null } = response;
  const validatedApiError = ApiErrorSchema.safeParse(error);
  if (!validatedApiError.success) {
    return {
      title: "Unknown server error response",
      status: 0,
      type: "0",
      Errors: [],
    };
  }
  return validatedApiError.data;
}

export function getFieldErrors(apiErrorList: ApiErrorList): Record<string, string[]> {
  const fieldErrors: Record<string, string[]> = {};
  apiErrorList.forEach((error) => {
    const key = getCamelCase(error.code);
    if (fieldErrors[key] == null)
      fieldErrors[key] = [];

    fieldErrors[key].push(error.message);
  });
  return fieldErrors;
}
