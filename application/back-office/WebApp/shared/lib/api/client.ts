import { createAuthenticationMiddleware } from "@repo/infrastructure/auth/AuthenticationMiddleware";
import { createAntiforgeryMiddleware } from "@repo/infrastructure/http/antiforgeryTokenHandler";
import { createErrorHandlerMiddleware } from "@repo/infrastructure/http/errorHandler";
import { MutationCache, QueryClient } from "@tanstack/react-query";
import createFetchClient from "openapi-fetch";
import createClient from "openapi-react-query";
import type { components, paths } from "./api.generated";

export * from "./api.generated.d";

export const apiClient = createFetchClient<paths>({
  baseUrl: import.meta.env.PUBLIC_URL
});

apiClient.use(createAuthenticationMiddleware());
apiClient.use(createAntiforgeryMiddleware());
apiClient.use(createErrorHandlerMiddleware());

export const api = createClient(apiClient);

export type Schemas = components["schemas"];

export const queryClient = new QueryClient({
  mutationCache: new MutationCache({
    onSuccess: () => {
      queryClient.invalidateQueries();
    }
  })
});
