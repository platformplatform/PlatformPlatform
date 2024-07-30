import type { ClientMethod, MaybeOptionalInit, ParseAsResponse } from "openapi-fetch";
import type {
  HttpMethod,
  MediaType,
  PathsWithMethod,
  SuccessResponse,
  ResponseObjectMap,
  HasRequiredKeys
} from "openapi-typescript-helpers";
import { ProblemDetailsError } from "./ProblemDetails";
import { parseServerErrorResponse } from "./HandleFluentValidationErrors";
import { createCachedClientMethod } from "./CachedClientMethod";

export function isHttpMethod(method: string | symbol): method is HttpMethod {
  return ["get", "put", "post", "delete", "options", "head", "patch", "trace"].includes(method.toString());
}

export type ClientMethodWithProblemDetails<
  // biome-ignore lint/complexity/noBannedTypes: This is the exact type used in "openapi-typescript"
  Paths extends Record<string, Record<HttpMethod, {}>>,
  Method extends HttpMethod,
  Media extends MediaType
> = <
  Path extends PathsWithMethod<Paths, Method>,
  Init extends MaybeOptionalInit<Paths[Path], Method>,
  Data = ParseAsResponse<SuccessResponse<ResponseObjectMap<Paths[Path][Method]>, Media>, Init>
>(
  url: Path,
  ...init: HasRequiredKeys<Init> extends never
    ? [(Init & { [key: string]: unknown })?] // note: the arbitrary [key: string]: addition MUST happen here after all the inference happens (otherwise TS can’t infer if it’s required or not)
    : [Init & { [key: string]: unknown }]
) => Promise<Data>;

export function createClientMethodWithProblemDetails<
  // biome-ignore lint/complexity/noBannedTypes: This is the exact type used in "openapi-typescript"
  Paths extends Record<string, Record<HttpMethod, {}>>,
  Method extends HttpMethod,
  Media extends MediaType = MediaType
>(func: ClientMethod<Paths, Method, Media>, shouldCache = false) {
  const clientMethodWithProblemDetails = async <
    Path extends PathsWithMethod<Paths, Method>,
    Init extends MaybeOptionalInit<Paths[Path], Method>,
    Data = ParseAsResponse<SuccessResponse<ResponseObjectMap<Paths[Path][Method]>, Media>, Init>
  >(
    ...params: Parameters<ClientMethod<Paths, Method, Media>>
  ): Promise<Data> => {
    try {
      const { data, error, response } = await func(...params);

      if (error) {
        const problemDetails = parseServerErrorResponse(error);
        if (problemDetails != null) {
          throw new ProblemDetailsError({
            status: response?.status,
            detail: response?.statusText !== "" ? response?.statusText : undefined,
            ...problemDetails
          });
        }

        // Invalid problem details response
        throw new ProblemDetailsError({
          type: "fetch-error",
          status: response.status,
          title: "An error occurred",
          detail: error.message ?? response.statusText
        });
      }

      return data as Data;
    } catch (error) {
      if (error instanceof ProblemDetailsError) {
        // Server validation errors
        if (import.meta.build_env.BUILD_TYPE === "development") console.warn("ProblemDetails", error.details);
        throw error;
      }
      if (error instanceof Error) {
        // Client error
        if (import.meta.build_env.BUILD_TYPE === "development") console.error("Error", error);
        throw new ProblemDetailsError({
          type: "tag:client-error",
          status: -1,
          title: "An error occurred",
          detail: error.message
        });
      }
      // Unknown error
      if (import.meta.build_env.BUILD_TYPE === "development") console.error("error", error);
      throw new ProblemDetailsError({
        type: "tag:unknown-error",
        status: -2,
        title: "An unknown error occurred",
        detail: `${error}`
      });
    }
  };

  if (shouldCache) {
    return createCachedClientMethod(clientMethodWithProblemDetails);
  }
  return clientMethodWithProblemDetails;
}
