import { z } from "zod";
import { i18n } from "@lingui/core";
import { accountManagementApi } from "./mock.api";
import { getApiError, getFieldErrors } from "@/shared/apiErrorListSchema";

export const tenantInfoScheme = z.object({
  value: z.string(),
});
export type TenantInfo = z.infer<typeof tenantInfoScheme>;

export const userRoleScheme = z.enum(["TenantUser", "TenantAdmin", "SuperAdmin"]);
export type UserRole = z.infer<typeof userRoleScheme>;

export const UserInfoScheme = z.object({
  isAuthenticated: z.boolean(),
  locale: z.string(),
  email: z.string().email().nullable().optional(),
  tenantId: z.string().nullable().optional(),
  userRole: userRoleScheme.nullable().optional(),
  userName: z.string().nullable().optional(),
});
export type UserInfo = z.infer<typeof UserInfoScheme>;

const validationResult = UserInfoScheme.safeParse(import.meta.user_info_env);

if (!validationResult.success) {
  console.error("Invalid user info", validationResult.error.flatten().fieldErrors);
  throw new Error("Invalid user info");
}

export const initialUserInfo: UserInfo = validationResult.data;

/**
 * Returns the user info if the user is authenticated or null if logged out
 * If user data is invalid, it will throw an error
 */
export async function getUserInfo(): Promise<UserInfo | null> {
  const { data, response } = await accountManagementApi.GET("/api/auth/user-info");
  if (!response.ok)
    return null;

  const validationResult = UserInfoScheme.safeParse(data);
  if (!validationResult.success)
    throw new Error("Invalid user info");

  return validationResult.data;
}

export interface State {
  success?: boolean;
  errors?: {
    email?: string[],
    password?: string[],
  };
  message?: string | null;
}

export const AuthenticateSchema = z.object({
  email: z.string().min(1, "Please enter your email").email("Please enter a valid email"),
  password: z.string().min(1, "Please enter your password"),
});

/**
 * Authenticates the user and returns a success state if successful
 * [FormAction]
 */
export async function authenticate(_: State, formData: FormData): Promise<State> {
  const validatedFields = AuthenticateSchema.safeParse({
    email: formData.get("email"),
    password: formData.get("password"),
  });

  if (!validatedFields.success) {
    // eslint-disable-next-line no-console
    console.log("validation errors", validatedFields.error.flatten().fieldErrors);
    return {
      errors: validatedFields.error.flatten().fieldErrors,
      message: i18n.t("Missing Fields. Failed to login."),
    };
  }

  const { email, password } = validatedFields.data;

  try {
    const result = await accountManagementApi.POST("/api/auth/login", {
      body: {
        email,
        password,
      },
    });

    if (result.response.ok) {
      return {
        success: true,
      };
    }

    const apiError = getApiError(result);

    return {
      message: apiError.title,
      errors: getFieldErrors(apiError.Errors),
    };
  }
  catch (e) {
    return {
      message: i18n.t("Server error: Failed login."),
    };
  }
}

/**
 * Logs the user out and returns a success state if successful
 * [FormAction]
 */
export async function logout(): Promise<State> {
  try {
    await accountManagementApi.POST("/api/auth/logout");
    return {
      success: true,
    };
  }
  catch (error) {
    return {
      message: i18n.t("Server error: Failed logout."),
    };
  }
}
