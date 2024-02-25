import { z } from "zod";
import { i18n } from "@lingui/core";
import { navigate } from "@/lib/router/router";
import { accountManagementApi } from "@/lib/api/client.ts";
import { getApiError, getFieldErrors } from "@/shared/apiErrorListSchema";

export interface State {
  errors?: {
    subdomain?: string[],
    name?: string[],
    email?: string[],
  };
  message?: string | null;
}

export const CreateTenantSchema = z.object({
  subdomain: z.string().min(1, "Please enter a subdomain").min(3, "Subdomain must be between 3-30 alphanumeric and lowercase characters"),
  name: z.string().min(1, "Please enter your name"),
});

export async function createTenant(_: State, formData: FormData): Promise<State> {
  const validatedFields = CreateTenantSchema.safeParse({
    subdomain: formData.get("subdomain"),
    name: formData.get("name"),
    email: formData.get("email"),
  });

  if (!validatedFields.success) {
    // eslint-disable-next-line no-console
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: i18n.t("Missing Fields. Failed to Create Account."),
    };
  }

  const { subdomain, name } = validatedFields.data;

  try {
    const result = await accountManagementApi.POST("/api/tenants", {
      body: {
        accountRegistrationId: "",
        subdomain,
        name,
      },
    });

    if (result.response.ok) {
      // invalidate cache
      // redirect
      navigate("/tenant/create/success");
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
      message: i18n.t("Server error: Failed to Create Account."),
    };
  }
}
