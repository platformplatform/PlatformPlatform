import { i18n } from "@lingui/core";
import { z } from "zod";
import { navigate } from "@/lib/router/router";
import { getApiError, getFieldErrors } from "@/shared/apiErrorListSchema";
import { accountManagementApi } from "@/lib/api/client";

const VALIDATION_LIFETIME = 1000 * 60 * 10; // 10 minutes

export interface State {
  errors?: { [key: string]: string | string[], };
  message?: string | null;
}

export const AccountRegistrationSchema = z.object({
  subdomain: z.string().min(3, "Subdomain must be between 3-30 alphanumeric and lowercase characters").max(30),
  email: z.string().min(5, "Please enter your email").email("Email must be in a valid format and no longer than 100 characters").max(100),
});

export async function registerAccount(_: State, formData: FormData): Promise<State> {
  const validatedFields = AccountRegistrationSchema.safeParse({
    subdomain: formData.get("subdomain"),
    email: formData.get("email"),
  });

  if (!validatedFields.success) {
    // eslint-disable-next-line no-console
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: i18n.t("Missing Fields. Failed to register account."),
    };
  }

  const { subdomain, email} = validatedFields.data;

  try {
    const result = await accountManagementApi.POST("/api/account-registrations/start", {
      body: {
        subdomain,
        email
      },
    });

    if (result.response.ok) {
      const location = result.response.headers.get("Location");
      if (!location) {
        return {
          message: i18n.t("Server error: Failed to register account."),
        };
      }
      const accountRegistrationId = location.split("/").pop();
      navigate(`/register/${accountRegistrationId}?email=${encodeURIComponent(email)}&expireAt=${Date.now() + VALIDATION_LIFETIME}`);
      return {};
    }

    const apiError = getApiError(result);

    return {
      message: apiError.title,
      errors: getFieldErrors(apiError.Errors),
    };
  }
  catch (e) {
    return {
      message: i18n.t("Server error: Failed to register account."),
    };
  }
}

const VerifyEmailSchema = z.object({
  accountRegistrationId: z.string().min(1, "Please enter your registration id"),
  oneTimePassword: z.string().min(6, "Please enter your verification code"),
});

export async function VerifyEmail(_: State, formData: FormData): Promise<State> {
  const validatedFields = VerifyEmailSchema.safeParse({
    accountRegistrationId: formData.get("accountRegistrationId"),
    oneTimePassword: formData.get("oneTimePassword"),
  });

  if (!validatedFields.success) {
    // eslint-disable-next-line no-console
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: i18n.t("Missing Fields. Failed to register account."),
    };
  }

  const { accountRegistrationId, oneTimePassword } = validatedFields.data;

  try {
    const result = await accountManagementApi.POST("/api/account-registrations/{id}/confirm-email", {
      params: {
        path: {
          // eslint-disable-next-line ts/ban-ts-comment
          // @ts-expect-error
          id: accountRegistrationId,
        },
      },
      body: {
        oneTimePassword,
      },
    });

    if (result.response.ok) {
      navigate("/dashboard");
      return {};
    }

    const apiError = getApiError(result);

    return {
      message: apiError.title,
      errors: getFieldErrors(apiError.Errors),
    };
  }
  catch (e) {
    return {
      message: i18n.t("Server error: Failed to resend verification."),
    };
  }
}
