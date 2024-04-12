import createClient from "openapi-fetch";
import type { paths } from "./api.generated";

export const accountManagementApi = createClient<paths>();
