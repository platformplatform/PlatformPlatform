import type { NavigateOptions } from "@tanstack/react-router";
import type React from "react";
import { createContext, useMemo, useState } from "react";

export type UserInfo = {
  initials: string;
  fullName: string;
} & UserInfoEnv;

export const initialUserInfo: UserInfo = createUserInfo({ ...import.meta.user_info_env });

export interface AuthenticationContextType {
  userInfo: UserInfo | null;
}

export const AuthenticationContext = createContext<AuthenticationContextType>({
  userInfo: initialUserInfo
});

export interface AuthenticationProviderProps {
  children: React.ReactNode;
  navigate?: (navigateOptions: NavigateOptions) => void;
}

/**
 * Provide authentication context to the application.
 */
export function AuthenticationProvider({ children }: Readonly<AuthenticationProviderProps>) {
  const [userInfo] = useState<UserInfo | null>(initialUserInfo);

  const authenticationContext = useMemo(
    () => ({
      userInfo
    }),
    [userInfo]
  );

  return <AuthenticationContext.Provider value={authenticationContext}>{children}</AuthenticationContext.Provider>;
}

function createUserInfo(userInfoEnv: UserInfoEnv): UserInfo {
  const { firstName, lastName, email } = userInfoEnv;

  const getInitial = (name: string | undefined) => name?.[0] ?? "";
  let initials = `${getInitial(firstName)}${getInitial(lastName)}`.toUpperCase();
  initials = initials != "" ? initials : email?.slice(0, 2).toUpperCase() ?? "";

  const fullName = firstName && lastName ? `${userInfoEnv.firstName} ${userInfoEnv.lastName}` : email ?? "";

  return { ...userInfoEnv, initials, fullName };
}
