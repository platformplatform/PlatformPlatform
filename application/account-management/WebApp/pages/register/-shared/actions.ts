import { i18n } from "@lingui/core";
import { getApiError, getFieldErrors } from "@repo/infrastructure/api/ErrorList";
import { accountManagementApi } from "@/shared/lib/api/client";
import type { FetchResponse } from "openapi-fetch";
import { z } from "zod";

interface CurrentRegistration {
  accountRegistrationId: string;
  email: string;
  expireAt: Date;
}

interface Registration {
  current: CurrentRegistration | undefined;
}

const registration: Registration = { current: undefined };

export function useRegistration(): CurrentRegistration {
  if (!registration.current) throw new Error("Account registration ID is missing.");
  return { ...registration.current };
}

export interface State {
  success?: boolean;
  message?: string | null;
  errors?: { [key: string]: string | string[] };
}

const accountRegistrationResponseSchema = z.object({
  accountRegistrationId: z.string(),
  validForSeconds: z.number()
});

export async function startAccountRegistration(_: State, formData: FormData): Promise<State> {
  const subdomain = formData.get("subdomain") as string;
  const email = formData.get("email") as string;

  const result = await accountManagementApi.POST("/api/account-management/account-registrations/start", {
    body: { subdomain, email }
  });

  if (!result.response.ok) {
    return convertResponseErrorToErrorState(result);
  }

  const { data, success } = accountRegistrationResponseSchema.safeParse(result.data);

  if (!success) {
    throw new Error("Start registration failed.");
  }

  registration.current = {
    accountRegistrationId: data.accountRegistrationId,
    email,
    expireAt: new Date(Date.now() + data.validForSeconds * 1000)
  };

  return { success: true };
}

export async function completeAccountRegistration(_: State, formData: FormData): Promise<State> {
  const oneTimePassword = formData.get("oneTimePassword") as string;
  const accountRegistrationId = registration.current?.accountRegistrationId;

  if (!accountRegistrationId) {
    return { success: false, message: i18n.t("Account registration is not started.") };
  }

  try {
    const result = await accountManagementApi.POST("/api/account-management/account-registrations/{id}/complete", {
      params: { path: { id: accountRegistrationId } },
      body: { oneTimePassword }
    });

    if (!result.response.ok) {
      return convertResponseErrorToErrorState(result);
    }

    return { success: true };
  } catch (e) {
    return {
      success: false,
      message: i18n.t("An error occured when trying to complete Account registration.")
    };
  }
}

type MediaType = `${string}/${string}`;

function convertResponseErrorToErrorState<T, O, M extends MediaType>(result: FetchResponse<T, O, M>): State {
  const apiError = getApiError(result);
  return { success: false, message: apiError.title, errors: getFieldErrors(apiError.Errors) };
}
