import createClient from "openapi-fetch";
import type { paths } from "./schema";

const baseUrl = "https://localhost:8443";
export const accountManagementApi = createClient<paths>({ baseUrl });
