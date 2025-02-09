import type { components, paths } from "./api.generated";
import { createPlatformApiClient } from "@repo/infrastructure/api/PlatformApiClient";
import { createAuthenticationMiddleware } from "@repo/infrastructure/auth/AuthenticationMiddleware";

import createFetchClient from "openapi-fetch";
import createClient from "openapi-react-query";

export * from "./api.generated.d";

export const api = createPlatformApiClient<paths>();
api.addMiddleware(createAuthenticationMiddleware());

export const useApi = api.useApi;

export const apiClient = createFetchClient<paths>({
  baseUrl: import.meta.env.PUBLIC_URL
});

export const newApi = createClient(apiClient);

export type Schemas = components["schemas"];
