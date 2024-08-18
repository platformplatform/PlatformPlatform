import type { NavigateOptions } from "@tanstack/react-router";
import type React from "react";
import { createContext, useCallback, useMemo, useRef, useState } from "react";

export type UserInfo = {
  initials: string;
  fullName: string;
} & UserInfoEnv;

export const initialUserInfo: UserInfo = createUserInfo({ ...import.meta.user_info_env });

/**
 * Returns the user info if the user is authenticated or null if logged out.
 * Throw an error if the user data is invalid.
 */
export async function getUserInfo(): Promise<UserInfo | null> {
  try {
    const response = await fetch("/api/auth/user-info");
    return createUserInfo(await response.json());
  } catch (error) {
    console.error("Failed to fetch user info", error);
    return null;
  }
}

export interface AuthenticationContextType {
  userInfo: UserInfo | null;
  reloadUserInfo: () => void;
}

export const AuthenticationContext = createContext<AuthenticationContextType>({
  userInfo: initialUserInfo,
  reloadUserInfo: () => {}
});

export interface AuthenticationProviderProps {
  children: React.ReactNode;
  navigate?: (navigateOptions: NavigateOptions) => void;
  afterLogOut?: NavigateOptions["to"];
  afterLogIn?: NavigateOptions["to"];
}

/**
 * Provide authentication context to the application.
 */
export function AuthenticationProvider({ children }: Readonly<AuthenticationProviderProps>) {
  const [userInfo, setUserInfo] = useState<UserInfo | null>(initialUserInfo);
  const fetching = useRef(false);

  const reloadUserInfo = useCallback(async () => {
    if (fetching.current) return;
    fetching.current = true;
    try {
      const newUserInfo = await getUserInfo();
      setUserInfo(newUserInfo);
    } catch (error) {
      setUserInfo(null);
    }
    fetching.current = false;
  }, []);

  const authenticationContext = useMemo(
    () => ({
      userInfo,
      reloadUserInfo
    }),
    [userInfo, reloadUserInfo]
  );

  return <AuthenticationContext.Provider value={authenticationContext}>{children}</AuthenticationContext.Provider>;
}

function createUserInfo(userInfoEnv: UserInfoEnv): UserInfo {
  const { firstName, lastName, email } = userInfoEnv;
  const initials = firstName && lastName ? `${firstName[0]}${lastName[0]}` : email?.slice(0, 2).toUpperCase() ?? "";

  const fullName = firstName && lastName ? `${userInfoEnv.firstName} ${userInfoEnv.lastName}` : email ?? "";

  return { ...userInfoEnv, initials, fullName };
}
