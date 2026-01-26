import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useCallback } from "react";

export function useMainNavigation() {
  const navigateToMain = useCallback((path: string) => {
    window.location.href = path;
  }, []);

  const navigateToHome = useCallback((returnPath?: string | null) => {
    window.location.href = returnPath ?? loggedInPath;
  }, []);

  return {
    navigateToMain,
    navigateToHome
  };
}
