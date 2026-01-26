import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { createContext, useCallback, useContext } from "react";

type NavigateToMainFn = (path: string) => void;

export const MainNavigationContext = createContext<NavigateToMainFn | null>(null);

export function useMainNavigation() {
  const contextNavigate = useContext(MainNavigationContext);

  if (!contextNavigate) {
    throw new Error("useMainNavigation must be used within MainNavigationContext.Provider");
  }

  const navigateToMain = useCallback(
    (path: string) => {
      contextNavigate(path);
    },
    [contextNavigate]
  );

  const navigateToHome = useCallback(
    (returnPath?: string | null) => {
      const targetPath = returnPath ?? loggedInPath;
      contextNavigate(targetPath);
    },
    [contextNavigate]
  );

  return {
    navigateToMain,
    navigateToHome
  };
}
