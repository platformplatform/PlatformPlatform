import { z } from "zod";
import { accountManagementApi } from "@/lib/api/client.ts";
import { getApiError, getFieldErrors } from "@/shared/apiErrorListSchema";
import { router } from "@/lib/router/router";

export type State = {
  errors?: {
    subdomain?: string[];
    name?: string[];
    email?: string[];
  };
  message?: string | null;
};

export const CreateTenantSchema = z.object({
  subdomain: z.string().min(1, "Please enter a subdomain").min(4, "Subdomain is required to be at least 4 characters"),
  name: z.string().min(1, "Please enter your name"),
  email: z.string().min(1, "Please enter your email").email("Please enter a valid email"),
});

export async function createTenant(_: State, formData: FormData): Promise<State> {
  const validatedFields = CreateTenantSchema.safeParse({
    subdomain: formData.get("subdomain"),
    name: formData.get("name"),
    email: formData.get("email"),
  });

  if (!validatedFields.success) {
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: "Missing Fields. Failed to Create Tenant.",
    };
  }

  const { subdomain, email, name } = validatedFields.data;

  try {
    const result = await accountManagementApi.POST("/api/tenants", {
      body: {
        subdomain,
        email,
        name,
      },
    });

    if (result.response.ok) {
      // invalidate cache
      // redirect
      router.navigate("/tenant/create/success");
      return {};
    }

    const apiError = getApiError(result);

    return {
      message: apiError.title,
      errors: getFieldErrors(apiError.Errors),
    };
  } catch (e) {
    return {
      message: "Server error: Failed to Create Tenant.",
    };
  }
}
