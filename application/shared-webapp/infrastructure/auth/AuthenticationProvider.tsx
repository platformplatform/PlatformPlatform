import type { NavigateOptions } from "@tanstack/react-router";
import type React from "react";

import { createContext, useCallback, useEffect, useMemo, useState } from "react";

import { subscribeToUserFeatureFlags } from "../featureFlags/userFeatureFlagsHeader";

export type UserInfo = {
  initials: string;
  fullName: string;
} & UserInfoEnv;

export const initialUserInfo: UserInfo = createUserInfo({ ...import.meta.user_info_env });

export interface AuthenticationContextType {
  userInfo: UserInfo | null;
  updateUserInfo: (newUserInfoEnv: Partial<UserInfoEnv>) => void;
}

export const AuthenticationContext = createContext<AuthenticationContextType>({
  userInfo: initialUserInfo,
  updateUserInfo: () => {}
});

export interface AuthenticationProviderProps {
  children: React.ReactNode;
  navigate: (navigateOptions: NavigateOptions) => void;
}

export function AuthenticationProvider({ children }: Readonly<AuthenticationProviderProps>) {
  const [userInfo, setUserInfo] = useState<UserInfo | null>(initialUserInfo);

  const updateUserInfo = useCallback((newUserInfoEnv: Partial<UserInfoEnv>) => {
    setUserInfo((prevUserInfo) => {
      const updatedUserInfoEnv = { ...prevUserInfo, ...newUserInfoEnv } as UserInfoEnv;
      return createUserInfo(updatedUserInfoEnv);
    });
  }, []);

  // Bridge the `x-user-feature-flags` response header into provider state. Only re-render when the
  // set of enabled keys actually changes; otherwise every API response would trigger a needless
  // re-render across every component using `useFeatureFlag`.
  useEffect(() => {
    return subscribeToUserFeatureFlags((nextFlagKeys) => {
      setUserInfo((prevUserInfo) => {
        if (prevUserInfo === null) return prevUserInfo;
        if (sameFlagKeys(prevUserInfo.featureFlags ?? [], nextFlagKeys)) return prevUserInfo;
        return createUserInfo({ ...prevUserInfo, featureFlags: nextFlagKeys });
      });
    });
  }, []);

  const authenticationContext = useMemo(
    () => ({
      userInfo,
      updateUserInfo
    }),
    [userInfo, updateUserInfo]
  );

  return <AuthenticationContext.Provider value={authenticationContext}>{children}</AuthenticationContext.Provider>;
}

function sameFlagKeys(previous: string[], next: string[]): boolean {
  if (previous.length !== next.length) return false;
  const previousSet = new Set(previous);
  for (const key of next) {
    if (!previousSet.has(key)) return false;
  }
  return true;
}

function createUserInfo(userInfoEnv: UserInfoEnv): UserInfo {
  const { firstName, lastName, email } = userInfoEnv;
  const getInitial = (name: string | undefined) => name?.[0] ?? "";
  let initials = `${getInitial(firstName)}${getInitial(lastName)}`.toUpperCase();
  initials = initials !== "" ? initials : (email?.slice(0, 2).toUpperCase() ?? "");
  const fullName = firstName || lastName ? `${userInfoEnv.firstName} ${userInfoEnv.lastName}` : (email ?? "");
  return { ...userInfoEnv, initials, fullName };
}
