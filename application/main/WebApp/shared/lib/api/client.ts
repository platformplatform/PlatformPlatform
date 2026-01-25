import { createApiClient } from "@repo/infrastructure/http/queryClient";
import type { components, paths } from "./api.generated";

export * from "./api.generated.d";
export type Schemas = components["schemas"];
export const { api, queryClient } = createApiClient<paths>();
