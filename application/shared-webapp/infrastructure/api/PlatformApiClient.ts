import type { Middleware, ClientOptions } from "openapi-fetch";
import type { MediaType, HttpMethod } from "openapi-typescript-helpers";
import createClient from "openapi-fetch";
import {
  type ClientMethodWithProblemDetails,
  createClientMethodWithProblemDetails,
  isHttpMethod
} from "./ClientMethodWithProblemDetails";
import { isKeyof } from "@repo/utils/object/isKeyof";
import { createPlatformServerAction, type PlatformServerAction } from "./PlatformServerAction";
import { createApiReactHook, type PlatformApiReactHook } from "./ApiReactHook";

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
  const useApi = createApiReactHook(client.GET);
  const notImplemented = (name: string | symbol) => {
    throw new Error(`Action client method not implemented: ${name.toString()}`);
  };
  return new Proxy({} as PlatformApiClient<Paths, Media>, {
    get(_, name) {
      if (name === "useApi") {
        return useApi;
      }
      if (name === "action") {
        return action;
      }
      if (isHttpMethod(name)) {
        const clientMethodKey = name.toUpperCase() as Uppercase<HttpMethod>;
        const shouldCache = clientMethodKey === "GET";
        return createClientMethodWithProblemDetails(client[clientMethodKey], shouldCache);
      }
      if (name === "addMiddleware") {
        return client.use;
      }
      if (name === "removeMiddleware") {
        return client.eject;
      }
      if (isKeyof(name, client)) {
        return client[name];
      }
      return notImplemented(name);
    }
  });
}

type PlatformApiClient<Paths extends {}, Media extends MediaType = MediaType> = {
  /** Call a GET endpoint using a React hook with state management */
  useApi: PlatformApiReactHook<Paths, "get", Media>;
  /** Call a server action */
  action: PlatformServerAction<Paths, "post", Media>;
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
  addMiddleware(...middleware: Middleware[]): void;
  /** Unregister middleware */
  removeMiddleware(...middleware: Middleware[]): void;
};
