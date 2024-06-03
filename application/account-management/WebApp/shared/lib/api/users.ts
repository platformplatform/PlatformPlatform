import type { FetchResponse } from "openapi-fetch";
import type { operations } from "./api.generated";
import { accountManagementApi } from "./client";

export async function getUsers(
  parameters: operations["GetApiAccountManagementUsers"]["parameters"]["query"]
): Promise<GetUsersResponse | null> {
  // biome-ignore lint/suspicious/noExplicitAny: <explanation>
  const result: FetchResponse<GetUsersResponse, any, any> = await accountManagementApi.GET(
    "/api/account-management/users",
    { params: { query: parameters } }
  );

  if (result.response.ok) return result.data;

  console.error(result.error);
  return null;
}

export interface GetUsersResponse {
  users: User[];
  totalPages: number;
  totalCount: number;
  currentPageOffset: number;
}

interface User {
  id: string;
  createdAt: string;
  modifiedAt: string | null;
  email: string;
  role: string;
  firstName: string | null;
  lastName: string | null;
  emailConfirmed: boolean;
  avatarUrl: string | null;
}
