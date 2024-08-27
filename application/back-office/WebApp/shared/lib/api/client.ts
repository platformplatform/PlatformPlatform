import type { components, paths } from "./api.generated";
import { createPlatformApiClient } from "@repo/infrastructure/api/PlatformApiClient";

export * from "./api.generated.d";

export const api = createPlatformApiClient<paths>();
export const useApi = api.useApi;

export type Schemas = components["schemas"];
