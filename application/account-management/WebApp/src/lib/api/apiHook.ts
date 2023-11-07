import { ZodiosHooks } from "@zodios/react";
import { createApiClient } from "./client";

const baseUrl = "https://localhost:8443/api";
export const apiClient = createApiClient(baseUrl);
export const apiHooks = new ZodiosHooks("accountmanagement", apiClient);
