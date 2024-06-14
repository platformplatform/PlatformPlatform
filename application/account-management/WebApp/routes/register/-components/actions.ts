import { i18n } from "@lingui/core";
import { z } from "zod";
import { getApiError, getFieldErrors } from "@/shared/apiErrorListSchema";
import { accountManagementApi } from "@/lib/api/client";

const VALIDATION_LIFETIME = 1000 * 60 * 5; // 5 minutes

interface CurrentRegistration {
  accountRegistrationId: string;
  email: string;
  expireAt: Date;
}

interface Registration {
  current: CurrentRegistration | undefined;
}

export const registration: Registration = {
  current: undefined,
};

export interface State {
  errors?: { [key: string]: string | string[], };
  message?: string | null;
  success?: boolean;
}

const StartAccountRegistrationSchema = z.object({
  subdomain: z.string().min(3, "Subdomain must be between 3-30 alphanumeric and lowercase characters").max(30),
  email: z.string().min(5, "Please enter your email").email("Email must be in a valid format and no longer than 100 characters").max(100),
});

export async function startAccountRegistration(_: State, formData: FormData): Promise<State> {
  registration.current = undefined;
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

  try {
    const result = await accountManagementApi.POST("/api/account-management/account-registrations/start", {
      body: {
        subdomain,
        email,
      },
    });

    if (!result.response.ok) {
      const apiError = getApiError(result);

      return {
        message: apiError.title,
        errors: getFieldErrors(apiError.Errors),
      };
    }

    const location = result.response.headers.get("Location");
    if (!location) {
      return {
        message: i18n.t("Server error: Failed to start account registration."),
      };
    }

    const accountRegistrationId = location.split("/").pop();

    if (!accountRegistrationId) {
      return {
        message: i18n.t("Server error: Failed to start account registration."),
      };
    }

    registration.current = {
      accountRegistrationId,
      email,
      expireAt: new Date(Date.now() + VALIDATION_LIFETIME),
    };
  }
  catch (e) {
    return {
      message: i18n.t("Server error: Failed to start account registration."),
    };
  }

  return {
    success: true,
  };
}

const CompleteAccountRegistrationSchema = z.object({
  oneTimePassword: z.string().min(6, "Please enter your verification code"),
});

export async function completeAccountRegistration(_: State, formData: FormData): Promise<State> {
  const validatedFields = CompleteAccountRegistrationSchema.safeParse({
    oneTimePassword: formData.get("oneTimePassword"),
  });

  if (!registration.current) {
    return {
      message: i18n.t("Account registration ID is missing."),
    };
  }

  if (!validatedFields.success) {
    // eslint-disable-next-line no-console
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: i18n.t("Missing Fields. Failed to complete account registration."),
    };
  }

  const { oneTimePassword } = validatedFields.data;
  const { accountRegistrationId } = registration.current;

  try {
    const result = await accountManagementApi.POST("/api/account-management/account-registrations/{id}/complete", {
      params: {
        path: {
          id: accountRegistrationId,
        },
      },
      body: {
        oneTimePassword,
      },
    });

    if (!result.response.ok) {
      const apiError = getApiError(result);

      return {
        message: apiError.title,
        errors: getFieldErrors(apiError.Errors),
      };
    }
  }
  catch (e) {
    return {
      message: i18n.t("Server error: Failed to complete account registration."),
    };
  }

  return {
    success: true,
  };
}
