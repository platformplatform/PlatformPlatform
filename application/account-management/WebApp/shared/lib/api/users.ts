import type { FetchResponse } from "openapi-fetch";
import type { operations, components } from "./api.generated";
import { accountManagementApi } from "./client";

export async function getUsers(
  parameters: operations["GetApiAccountManagementUsers"]["parameters"]["query"]
): Promise<components['schemas']['GetUsersResponseDto'] | null> {
  // biome-ignore lint/suspicious/noExplicitAny: <explanation>
  const result: FetchResponse<components['schemas']['GetUsersResponseDto'], any, any> = await accountManagementApi.GET(
    "/api/account-management/users",
    { params: { query: parameters } }
  );

  if (result.response.ok) return result.data;

  console.error(result.error);
  return null;
}

