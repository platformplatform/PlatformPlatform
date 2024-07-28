import type { ClientMethod, Middleware, MaybeOptionalInit, ParseAsResponse, ClientOptions } from "openapi-fetch";
import type {
  MediaType,
  HttpMethod,
  PathsWithMethod,
  SuccessResponse,
  ResponseObjectMap
} from "openapi-typescript-helpers";
import createClient from "openapi-fetch";
import type { FormProps } from "react-aria-components";
import {
  type ClientMethodWithProblemDetails,
  createClientMethodWithProblemDetails,
  isHttpMethod,
  ProblemDetailsError
} from "./ClientMethodWithProblemDetails";

/**
 * Create a client for the platform API.
 * We use the openapi-fetch library to create a client for the platform API.
 * In addition to the standard client methods, we also provide a method to call server actions.
 *
 * Due to __"operational consistency"__ in `PlatformPlatform` all Api calls have a uniform behavior.
 * * `baseUrl` is set to the `import.meta.env.PUBLIC_URL` per default *(but can be overridden)*
 * * Data is returned by the api client otherwise errors are thrown as either `Error` or `ProblemDetailsError`
 * * Use the http methods `get`, `put`, `post`, `delete`, `options`, `head`, `patch`, `trace` to call the api
 * * Use the `action` method to call server actions *(Useful for interacting with Form data in React)*
 *
 * In PlatformPlatform all Api calls use the ProblemDetails standard and throw errors.
 */
export function createPlatformApiClient<Paths extends {}, Media extends MediaType = MediaType>(
  clientOptions?: ClientOptions
) {
  const client = createClient<Paths>({
    baseUrl: import.meta.env.PUBLIC_URL,
    ...clientOptions
  });
  const action = createPlatformServerAction(client.POST);
  const notImplemented = (name: string | symbol) => {
    throw new Error(`Action client method not implemented: ${name.toString()}`);
  };
  return new Proxy({} as PlatformApiClient<Paths, Media>, {
    get(_, name) {
      if (name === "action") {
        return action;
      }
      if (isHttpMethod(name)) {
        const clientMethodKey = name.toUpperCase() as Uppercase<HttpMethod>;
        return createClientMethodWithProblemDetails(client[clientMethodKey]);
      }
      if (isKeyof(name, client)) {
        return client[name];
      }
      return notImplemented(name);
    }
  });
}

function createPlatformServerAction<
  // biome-ignore lint/complexity/noBannedTypes: This is the exact type used in "openapi-typescript"
  Paths extends Record<string, Record<HttpMethod, {}>>,
  Media extends MediaType = MediaType
>(clientMethod: ClientMethod<Paths, "post", Media>) {
  const postMethod = createClientMethodWithProblemDetails(clientMethod);
  return <Path extends PathsWithMethod<Paths, "post">, Data = Awaited<ReturnType<typeof postMethod>>>(pathname: Path) =>
    async (_: ActionClientState, formData: FormData): Promise<ActionClientState<Data>> => {
      try {
        const body = Object.fromEntries(formData);
        const data = postMethod(pathname, {
          // Make body data available for path parameters
          path: body,
          body
          // biome-ignore lint/suspicious/noExplicitAny: We don't know the type at this point
        } as any) as Data;

        return { success: true, data, message: undefined, errors: undefined };
      } catch (error) {
        if (error instanceof ProblemDetailsError) {
          // Server validation errors
          return {
            success: false,
            message: error.details.title,
            errors: error.details.errors,
            data: undefined
          };
        }
        if (error instanceof Error) {
          // API error
          return { success: false, message: error.message, data: undefined, errors: {} };
        }
        // Unknown error
        return { success: false, message: "An error occurred.", data: undefined, errors: {} };
      }
    };
}

function isKeyof<O extends {}>(key: string | symbol | keyof O, object: O): key is keyof O {
  return typeof key === "string" && key in object;
}

type PlatformServerAction<
  // biome-ignore lint/complexity/noBannedTypes: This is the exact type used in "openapi-typescript"
  Paths extends Record<string, Record<HttpMethod, {}>>,
  Media extends MediaType = MediaType,
  Method extends HttpMethod = "post"
> = <
  Path extends PathsWithMethod<Paths, Method>,
  Init extends MaybeOptionalInit<Paths[Path], Method>,
  // Data = ParseAsResponse<SuccessResponse<ResponseObjectMap<Paths[Path][Method]>, Media>, Init>
  T = Paths[Path][Method],
  Options = Init,
  Data = ParseAsResponse<SuccessResponse<ResponseObjectMap<T>, Media>, Options>
>(
  p: Path
) => (_: ActionClientState, formData: FormData) => Promise<ActionClientState<Data>>;

type PlatformApiClient<Paths extends {}, Media extends MediaType = MediaType> = {
  action: PlatformServerAction<Paths, Media>;
  /** Call a GET endpoint */
  get: ClientMethodWithProblemDetails<Paths, "get", Media>;
  /** Call a PUT endpoint */
  put: ClientMethodWithProblemDetails<Paths, "put", Media>;
  /** Call a POST endpoint */
  post: ClientMethodWithProblemDetails<Paths, "post", Media>;
  /** Call a DELETE endpoint */
  delete: ClientMethodWithProblemDetails<Paths, "delete", Media>;
  /** Call a OPTIONS endpoint */
  options: ClientMethodWithProblemDetails<Paths, "options", Media>;
  /** Call a HEAD endpoint */
  head: ClientMethodWithProblemDetails<Paths, "head", Media>;
  /** Call a PATCH endpoint */
  patch: ClientMethodWithProblemDetails<Paths, "patch", Media>;
  /** Call a TRACE endpoint */
  trace: ClientMethodWithProblemDetails<Paths, "trace", Media>;
  /** Register middleware */
  use(...middleware: Middleware[]): void;
  /** Unregister middleware */
  eject(...middleware: Middleware[]): void;
};

export type ValidationErrors = NonNullable<FormProps["validationErrors"]>;

export type ActionClientState<D = unknown> =
  | {
      success?: null;
      message?: string;
      errors?: ValidationErrors;
      data?: null;
    }
  | {
      success: false;
      message: string;
      errors: ValidationErrors;
      data: undefined;
    }
  | {
      success: true;
      message: undefined;
      errors: undefined;
      data: D;
    };
