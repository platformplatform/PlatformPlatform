import type { paths, components } from "./api.generated";
import { createPlatformApiClient } from "@repo/infrastructure/api/PlatformApiClient";
import { createAuthenticationMiddleware } from "@repo/infrastructure/auth/AuthenticationMiddleware";
export * from "./api.generated.d";

export const api = createPlatformApiClient<paths>();
api.addMiddleware(createAuthenticationMiddleware({ loginPath: "/login" }));

export const useApi = api.useApi;

export type Schemas = components["schemas"];
