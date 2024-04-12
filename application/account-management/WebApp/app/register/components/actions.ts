"use client";
import { i18n } from "@lingui/core";
import { z } from "zod";
import { redirect } from "next/navigation";
import { getApiError, getFieldErrors } from "@/shared/apiErrorListSchema";
import { accountManagementApi } from "@/lib/api/client";

const VALIDATION_LIFETIME = 1000 * 60 * 5; // 5 minutes

export interface State {
  errors?: { [key: string]: string | string[], };
  message?: string | null;
}

const StartAccountRegistrationSchema = z.object({
  subdomain: z.string().min(3, "Subdomain must be between 3-30 alphanumeric and lowercase characters").max(30),
  email: z.string().min(5, "Please enter your email").email("Email must be in a valid format and no longer than 100 characters").max(100),
});

export async function startAccountRegistration(_: State, formData: FormData): Promise<State> {
  const validatedFields = StartAccountRegistrationSchema.safeParse({
    subdomain: formData.get("subdomain"),
    email: formData.get("email"),
  });

  if (!validatedFields.success) {
    // eslint-disable-next-line no-console
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: i18n.t("Missing Fields. Failed to start account registration."),
    };
  }

  const { subdomain, email } = validatedFields.data;

  const result = await accountManagementApi.POST("/api/account-registrations/start", {
    body: {
      subdomain,
      email,
    },
  });

  if (result.response.ok) {
    const location = result.response.headers.get("Location");
    if (!location) {
      return {
        message: i18n.t("Server error: Failed to start account registration."),
      };
    }
    const accountRegistrationId = location.split("/").pop();
    redirect(`/register/${accountRegistrationId}?email=${encodeURIComponent(email)}&expireAt=${Date.now() + VALIDATION_LIFETIME}`);
  }

  const apiError = getApiError(result);

  return {
    message: apiError.title,
    errors: getFieldErrors(apiError.Errors),
  };
}

const CompleteAccountRegistrationSchema = z.object({
  accountRegistrationId: z.string().min(1, "Please enter your account registration id"),
  oneTimePassword: z.string().min(6, "Please enter your verification code"),
});

export async function completeAccountRegistration(_: State, formData: FormData): Promise<State> {
  const validatedFields = CompleteAccountRegistrationSchema.safeParse({
    accountRegistrationId: formData.get("accountRegistrationId"),
    oneTimePassword: formData.get("oneTimePassword"),
  });

  if (!validatedFields.success) {
    // eslint-disable-next-line no-console
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: i18n.t("Missing Fields. Failed to complete account registration."),
    };
  }

  const { accountRegistrationId, oneTimePassword } = validatedFields.data;

  const result = await accountManagementApi.POST("/api/account-registrations/{id}/complete", {
    params: {
      path: {
        id: accountRegistrationId,
      },
    },
    body: {
      oneTimePassword,
    },
  });

  if (result.response.ok) {
    redirect("/dashboard");
  }

  const apiError = getApiError(result);

  return {
    message: apiError.title,
    errors: getFieldErrors(apiError.Errors),
  };
}
