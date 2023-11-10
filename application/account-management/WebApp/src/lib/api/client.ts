import createClient from "openapi-fetch";
import type { paths } from "./api.generated";

const baseUrl = "https://localhost:8443";
export const accountManagementApi = createClient<paths>({ baseUrl });
