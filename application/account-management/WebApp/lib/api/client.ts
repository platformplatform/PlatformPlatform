import createClient from "openapi-fetch";
import type { paths } from "./api.generated";

const baseUrl = "";
export const accountManagementApi = createClient<paths>({ baseUrl });
