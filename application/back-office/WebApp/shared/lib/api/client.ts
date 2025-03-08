import { createAuthenticationMiddleware } from "@repo/infrastructure/auth/AuthenticationMiddleware";
import type { components, paths } from "./api.generated";

import createFetchClient from "openapi-fetch";
import createClient from "openapi-react-query";

export * from "./api.generated.d";

export const apiClient = createFetchClient<paths>({
  baseUrl: import.meta.env.PUBLIC_URL
});
apiClient.use(createAuthenticationMiddleware());

export const api = createClient(apiClient);

export type Schemas = components["schemas"];
