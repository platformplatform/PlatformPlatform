import { i18n } from "@lingui/core";
import { getApiError, getFieldErrors } from "@repo/infrastructure/api/ErrorList";
import { accountManagementApi } from "@/shared/lib/api/client";

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
    const apiError = getApiError(result);
    let errorState = { success: false, message: apiError.title, errors: getFieldErrors(apiError.Errors) };
    console.log(errorState);
    return errorState;
  }

  try {
    const location = result.response.headers.get("Location")!;
    const accountRegistrationId = location.split("/").pop()!;

    registration.current = { accountRegistrationId, email, expireAt: new Date(Date.now() + VALIDATION_LIFETIME) };

    return { success: true };
  } catch (e) {
    return { success: false, message: i18n.t("An error occured when trying to start Account registration.") };
  }
}

export async function completeAccountRegistration(_: State, formData: FormData): Promise<State> {
  const oneTimePassword = formData.get("oneTimePassword") as string;
  const accountRegistrationId = registration.current?.accountRegistrationId!;

  try {
    const result = await accountManagementApi.POST("/api/account-management/account-registrations/{id}/complete", {
      params: { path: { id: accountRegistrationId } },
      body: { oneTimePassword }
    });

    if (!result.response.ok) {
      const apiError = getApiError(result);
      let errorState = { success: false, message: apiError.title, errors: getFieldErrors(apiError.Errors) };
      console.log(errorState);
      return errorState;
    }

    return { success: true };
  } catch (e) {
    return { success: false, message: i18n.t("An error occured when trying to complete Account registration.") };
  }
}
