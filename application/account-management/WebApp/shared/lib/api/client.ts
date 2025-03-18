import { createAuthenticationMiddleware } from "@repo/infrastructure/auth/AuthenticationMiddleware";
import type { components, paths } from "./api.generated";

import createFetchClient from "openapi-fetch";
import createClient from "openapi-react-query";

export * from "./api.generated.d";

const apiClient = createFetchClient<paths>({
  baseUrl: import.meta.env.PUBLIC_URL
});
apiClient.use(createAuthenticationMiddleware());

// Add middleware to include antiforgery token only for non-GET requests
apiClient.use({
  onRequest: (params) => {
    const request = params.request;
    if (request instanceof Request && request.method !== "GET") {
      request.headers.set("x-xsrf-token", getAntiforgeryToken());
    }
    return request;
  }
});

// Get the antiforgery token from the meta tag
const getAntiforgeryToken = () => {
  const metaTag = document.querySelector('meta[name="antiforgeryToken"]');
  return metaTag?.getAttribute("content") ?? "";
};

export const api = createClient(apiClient);

export type Schemas = components["schemas"];
