import type { ClientMethod, MaybeOptionalInit, ParseAsResponse } from "openapi-fetch";
import type {
  HttpMethod,
  MediaType,
  PathsWithMethod,
  ResponseObjectMap,
  SuccessResponse
} from "openapi-typescript-helpers";
import type { FormProps } from "react-aria-components";
import { createClientMethodWithProblemDetails } from "./ClientMethodWithProblemDetails";
import { ProblemDetailsError } from "./ProblemDetails";

export type ValidationErrors = NonNullable<FormProps["validationErrors"]>;

export type ActionClientState<D = unknown> =
  | {
      success?: null;
      data?: null;
      // Problem details
      type?: string;
      status?: number;
      title?: string;
      message?: string;
      errors?: ValidationErrors;
    }
  | {
      success: false;
      data: undefined;
      // Problem details
      type?: string;
      status?: number;
      title?: string;
      message?: string;
      errors?: ValidationErrors;
    }
  | {
      success: true;
      data: D;
      // Problem details
      type: undefined;
      status: undefined;
      title: undefined;
      message: undefined;
      errors: undefined;
    };

export function createPlatformServerAction<
  // biome-ignore lint/complexity/noBannedTypes: This is the exact type used in "openapi-typescript"
  Paths extends Record<string, Record<HttpMethod, {}>>,
  Method extends HttpMethod,
  Media extends MediaType = MediaType
>(clientMethod: ClientMethod<Paths, Method, Media>) {
  const postMethod = createClientMethodWithProblemDetails(clientMethod);
  return <
    Path extends PathsWithMethod<Paths, Method>,
    Init extends MaybeOptionalInit<Paths[Path], Method>,
    T = Paths[Path][Method],
    Options = Init,
    Data = ParseAsResponse<SuccessResponse<ResponseObjectMap<T>, Media>, Options>
  >(
    templateUrl: Path
  ) =>
    async (_: ActionClientState, formData: FormData): Promise<ActionClientState<Data>> => {
      try {
        const body = Object.fromEntries(formData);
        const paramPaths: Record<string, string> = {};
        const pathParamNames = getParamNamesFromTemplateUrl(templateUrl.toString());

        for (const paramName of pathParamNames) {
          const value = formData.get(paramName);
          if (value == null) {
            throw new Error(`Missing path parameter: ${paramName}`);
          }
          if (typeof value !== "string") {
            throw new Error(`Invalid path parameter: ${paramName}, type: ${typeof value}`);
          }
          paramPaths[paramName] = value;
          delete body[paramName];
        }

        const data = (await postMethod(templateUrl, {
          // Make body data available for path parameters
          params: {
            path: paramPaths
          },
          body
          // biome-ignore lint/suspicious/noExplicitAny: We don't know the type at this point
        } as any)) as Data;

        return {
          success: true,
          data,
          type: undefined,
          status: undefined,
          title: undefined,
          message: undefined,
          errors: undefined
        };
      } catch (error) {
        if (error instanceof ProblemDetailsError) {
          // The api client should always return a ProblemDetailsError
          return {
            success: false,
            data: undefined,
            type: error.details.type,
            status: error.details.status,
            title: error.details.title,
            message: error.details.detail,
            errors: error.details.errors
          };
        }
        if (error instanceof Error) {
          // Unknown error
          return {
            success: false,
            type: "",
            status: -1,
            title: error.name,
            message: error.message,
            data: undefined,
            errors: {}
          };
        }
        // Unknown error
        return {
          success: false,
          type: "",
          status: -2,
          title: "Unknown error",
          message: "An error occurred.",
          data: undefined,
          errors: {}
        };
      }
    };
}

export type PlatformServerAction<
  // biome-ignore lint/complexity/noBannedTypes: This is the exact type used in "openapi-typescript"
  Paths extends Record<string, Record<HttpMethod, {}>>,
  Method extends HttpMethod,
  Media extends MediaType = MediaType
> = ReturnType<typeof createPlatformServerAction<Paths, Method, Media>>;

function getParamNamesFromTemplateUrl(templateUrl: string): string[] {
  return templateUrl.match(/{([^}]+)}/g)?.map((match) => match.slice(1, -1)) ?? [];
}
