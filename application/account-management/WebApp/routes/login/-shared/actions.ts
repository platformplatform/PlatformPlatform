import { i18n } from "@lingui/core";
import { getApiError, getFieldErrors } from "@repo/infrastructure/api/ErrorList";
import { accountManagementApi } from "@/shared/lib/api/client";
import type { FetchResponse } from "openapi-fetch";

interface CurrentLogin {
  loginId: string;
  email: string;
  expireAt: Date;
}

interface Login {
  current: CurrentLogin | undefined;
}

const login: Login = { current: undefined };

export function useLogin(): CurrentLogin {
  if (!login.current) throw new Error("Login ID is missing.");
  return { ...login.current };
}

export interface State {
  success?: boolean;
  message?: string | null;
  errors?: { [key: string]: string | string[] };
}

export async function startLogin(_: State, formData: FormData): Promise<State> {
  const email = formData.get("email") as string;

  const result = await accountManagementApi.POST("/api/account-management/authentication/start", {
    body: { email }
  });

  if (!result.response.ok) {
    return convertResponseErrorToErrorState(result);
  }

  if (!result.data) {
    throw new Error("Start login failed.");
  }

  login.current = {
    loginId: result.data.loginId as string,
    email,
    expireAt: new Date(Date.now() + (result.data.validForSeconds as number) * 1000)
  };

  return { success: true };
}

export async function completeLogin(_: State, formData: FormData): Promise<State> {
  const oneTimePassword = formData.get("oneTimePassword") as string;
  const loginId = login.current?.loginId;

  if (!loginId) {
    return { success: false, message: i18n.t("Login is not started.") };
  }

  try {
    const result = await accountManagementApi.POST("/api/account-management/authentication/{id}/complete", {
      params: { path: { id: loginId } },
      body: { oneTimePassword }
    });

    if (!result.response.ok) {
      return convertResponseErrorToErrorState(result);
    }

    return { success: true };
  } catch (e) {
    return {
      success: false,
      message: i18n.t("An error occured when trying to complete login.")
    };
  }
}

type MediaType = `${string}/${string}`;

function convertResponseErrorToErrorState<T, O, M extends MediaType>(result: FetchResponse<T, O, M>): State {
  const apiError = getApiError(result);
  return { success: false, message: apiError.title, errors: getFieldErrors(apiError.Errors) };
}
