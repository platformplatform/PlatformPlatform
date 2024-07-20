import type { NavigateOptions } from "@tanstack/react-router";
import { createContext, useCallback, useMemo, useRef, useState } from "react";
import type { AuthenticationState, UserInfo } from "./actions";
import { authenticate, getUserInfo, initialUserInfo, logout } from "./actions";

export interface AuthenticationContextType {
  userInfo: UserInfo | null;
  reloadUserInfo: () => void;
  logInAction: (_: AuthenticationState, formData: FormData) => Promise<AuthenticationState>;
  logOutAction: () => Promise<AuthenticationState>;
}

export const AuthenticationContext = createContext<AuthenticationContextType>({
  userInfo: initialUserInfo,
  reloadUserInfo: () => {},
  logInAction: async () => ({}),
  logOutAction: async () => ({})
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
export function AuthenticationProvider({
  children,
  navigate,
  afterLogIn,
  afterLogOut
}: Readonly<AuthenticationProviderProps>) {
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

  const logOutAction = useCallback(async () => {
    const result = await logout();
    setUserInfo(null);
    if (navigate && afterLogOut) navigate({ to: afterLogOut });

    return result;
  }, [navigate, afterLogOut]);

  const logInAction = useCallback(
    async (state: AuthenticationState, formData: FormData) => {
      const result = await authenticate(state, formData);
      if (result.success) setUserInfo(await getUserInfo());

      if (result.success && navigate && afterLogIn) navigate({ to: afterLogIn });
      return result;
    },
    [navigate, afterLogIn]
  );

  const authenticationContext = useMemo(
    () => ({
      userInfo,
      reloadUserInfo,
      logInAction,
      logOutAction
    }),
    [userInfo, reloadUserInfo, logInAction, logOutAction]
  );

  return <AuthenticationContext.Provider value={authenticationContext}>{children}</AuthenticationContext.Provider>;
}
