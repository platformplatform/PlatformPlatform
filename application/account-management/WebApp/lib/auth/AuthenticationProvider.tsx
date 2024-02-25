import { createContext, useCallback, useMemo, useRef, useState } from "react";
import { authenticate, getUserInfo, initialUserInfo, logout } from "./actions";
import type { State, UserInfo } from "./actions";

export interface AuthenticationContextType {
  userInfo: UserInfo | null;
  reloadUserInfo: () => void;
  signInAction: (_: State, formData: FormData) => Promise<State>;
  signOutAction: () => Promise<State>;
}

export const AuthenticationContext = createContext<AuthenticationContextType>({
  userInfo: initialUserInfo,
  reloadUserInfo: () => {},
  signInAction: async () => ({}),
  signOutAction: async () => ({}),
});

export interface AuthenticationProviderProps {
  children: React.ReactNode;
  navigate?: (path: string) => void;
  afterSignOut?: string;
  afterSignIn?: string;
};

/**
 * Provide authentication context to the application.
 */
export function AuthenticationProvider({
  children,
  navigate,
  afterSignIn,
  afterSignOut,
}: Readonly<AuthenticationProviderProps>) {
  const [userInfo, setUserInfo] = useState<UserInfo | null>(initialUserInfo);
  const fetching = useRef(false);

  const reloadUserInfo = useCallback(async () => {
    if (fetching.current)
      return;
    fetching.current = true;
    try {
      const newUserInfo = await getUserInfo();
      setUserInfo(newUserInfo);
    }
    catch (error) {
      setUserInfo(null);
    }
    fetching.current = false;
  }, [setUserInfo]);

  const signOutAction = useCallback(async () => {
    const result = await logout();
    setUserInfo(null);
    if (navigate && afterSignOut)
      navigate(afterSignOut);
    return result;
  }, [setUserInfo, navigate, afterSignOut]);

  const signInAction = useCallback(async (state: State, formData: FormData) => {
    const result = await authenticate(state, formData);
    if (result.success)
      setUserInfo(await getUserInfo());

    if (result.success && navigate && afterSignIn)
      navigate(afterSignIn);
    return result;
  }, [navigate, afterSignIn]);

  const authenticationContext = useMemo(() =>
    ({
      userInfo,
      reloadUserInfo,
      signInAction,
      signOutAction,
    }), [userInfo, reloadUserInfo, signInAction, signOutAction]);

  return (
    <AuthenticationContext.Provider value={authenticationContext}>
      {children}
    </AuthenticationContext.Provider>
  );
};
