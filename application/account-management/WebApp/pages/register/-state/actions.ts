import { i18n } from "@lingui/core";
import { getApiError, getFieldErrors } from "@repo/infrastructure/api/ErrorList";
import { accountManagementApi } from "@/shared/lib/api/client";
import type { FetchResponse } from "openapi-fetch";

const VALIDATION_LIFETIME = 1000 * 60 * 5; // 5 minutes

interface CurrentRegistration {
  accountRegistrationId: string;
  email: string;
  expireAt: Date;
}

interface Registration {
  current: CurrentRegistration | undefined;
}

export const registration: Registration = { current: undefined };

export interface State {
  error: boolean;
  success?: boolean;
  message?: string | null;
  errors?: { [key: string]: string | string[] };
}

export async function startAccountRegistration(_: State, formData: FormData): Promise<State> {
  const subdomain = formData.get("subdomain") as string;
  const email = formData.get("email") as string;

  const result = await accountManagementApi.POST("/api/account-management/account-registrations/start", {
    body: { subdomain, email }
  });

  if (!result.response.ok) {
    const errorState = convertResponseErrorToErrorState(result);
    console.log(errorState);
    return errorState;
  }

  try {
    const accountRegistrationId = extractRegistrationIdFromHeader(result.response.headers);

    if (!accountRegistrationId) {
      return {
        error: true,
        success: false,
        message: i18n.t("An error occured when trying to start Account registration.")
      };
    }

    registration.current = { accountRegistrationId, email, expireAt: new Date(Date.now() + VALIDATION_LIFETIME) };

    return { error: false, success: true };
  } catch (e) {
    return {
      error: true,
      success: false,
      message: i18n.t("An error occured when trying to start Account registration.")
    };
  }
}

export async function completeAccountRegistration(_: State, formData: FormData): Promise<State> {
  const oneTimePassword = formData.get("oneTimePassword") as string;
  const accountRegistrationId = registration.current?.accountRegistrationId;

  if (!accountRegistrationId) {
    return { error: true, success: false, message: i18n.t("Account registration is not started.") };
  }

  try {
    const result = await accountManagementApi.POST("/api/account-management/account-registrations/{id}/complete", {
      params: { path: { id: accountRegistrationId } },
      body: { oneTimePassword }
    });

    if (!result.response.ok) {
      const errorState = convertResponseErrorToErrorState(result);
      console.log(errorState);
      return errorState;
    }

    return { error: false, success: true };
  } catch (e) {
    return {
      error: true,
      success: false,
      message: i18n.t("An error occured when trying to complete Account registration.")
    };
  }
}

type MediaType = `${string}/${string}`;

function convertResponseErrorToErrorState<T, O, M extends MediaType>(result: FetchResponse<T, O, M>): State {
  const apiError = getApiError(result);
  return { error: true, success: false, message: apiError.title, errors: getFieldErrors(apiError.Errors) };
}

function extractRegistrationIdFromHeader(headers: Headers): string | undefined {
  const location = headers.get("Location");
  return location?.split("/").pop();
}
