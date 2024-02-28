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

export const RegisterSchema = z.object({
  email: z.string().min(1, "Please enter your email").email("Please enter a valid email"),
  firstName: z.string().min(1, "Please enter your first name"),
  lastName: z.string().min(1, "Please enter your last name"),
});

export async function register(_: State, formData: FormData): Promise<State> {
  const validatedFields = RegisterSchema.safeParse({
    email: formData.get("email"),
    firstName: formData.get("firstName"),
    lastName: formData.get("lastName"),
  });

  if (!validatedFields.success) {
    // eslint-disable-next-line no-console
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: i18n.t("Missing Fields. Failed to register."),
    };
  }

  const { email, firstName, lastName } = validatedFields.data;

  try {
    const result = await accountManagementApi.POST("/api/account-registrations/start", {
      body: {
        email,
        firstName,
        lastName,
      },
    });

    if (result.response.ok) {
      const location = result.response.headers.get("Location");
      if (!location) {
        return {
          message: i18n.t("Server error: Failed register."),
        };
      }
      const registrationId = location.split("/").pop();
      navigate(`/register/${registrationId}?email=${encodeURIComponent(email)}&expireAt=${Date.now() + VALIDATION_LIFETIME}`);
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
      message: i18n.t("Server error: Failed register."),
    };
  }
}

const VerifyEmailSchema = z.object({
  registrationId: z.string().min(1, "Please enter your registration id"),
  oneTimePassword: z.string().min(6, "Please enter your verification code"),
});

export async function VerifyEmail(_: State, formData: FormData): Promise<State> {
  const validatedFields = VerifyEmailSchema.safeParse({
    registrationId: formData.get("registrationId"),
    oneTimePassword: formData.get("oneTimePassword"),
  });

  if (!validatedFields.success) {
    // eslint-disable-next-line no-console
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: i18n.t("Missing Fields. Failed to register."),
    };
  }

  const { registrationId, oneTimePassword } = validatedFields.data;

  try {
    const result = await accountManagementApi.POST("/api/account-registrations/{id}/confirm-email", {
      params: {
        path: {
          // eslint-disable-next-line ts/ban-ts-comment
          // @ts-expect-error
          id: registrationId,
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
