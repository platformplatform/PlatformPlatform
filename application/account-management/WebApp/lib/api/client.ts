import createClient from "openapi-fetch";
import type { paths } from "./api.generated";

const baseUrl = "/api/account-management";
export const accountManagementApi = createClient<paths>({ baseUrl });
