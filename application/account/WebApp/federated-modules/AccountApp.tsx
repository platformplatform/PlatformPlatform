import "@repo/ui/tailwind.css";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { createBlockableMemoryHistory } from "@repo/infrastructure/router/createBlockableMemoryHistory";
import { shouldBlockNavigation } from "@repo/ui/hooks/federatedNavigationGuard";
import { createRouter, type NavigateOptions, RouterProvider } from "@tanstack/react-router";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { UnsavedChangesDialog } from "../shared/components/UnsavedChangesDialog";
import { MainNavigationContext } from "../shared/hooks/useMainNavigation";
import { routeTree } from "../shared/lib/router/routeTree.generated";

export interface AccountAppProps {
  initialPath: string;
  onNavigateToMain: (path: string) => void;
}

export default function AccountApp({ initialPath, onNavigateToMain }: Readonly<AccountAppProps>) {
  // Store initialPath in a ref to prevent router recreation when main's router re-renders
  // due to URL changes from window.history.pushState() calls within this component
  const initialPathRef = useRef(initialPath);

  const router = useMemo(() => {
    const memoryHistory = createBlockableMemoryHistory({ initialEntries: [initialPathRef.current] });
    return createRouter({
      routeTree,
      history: memoryHistory,
      defaultPreload: "intent"
    });
  }, []);

  useEffect(() => {
    return router.subscribe("onResolved", ({ toLocation }) => {
      const newPath = toLocation.pathname + (toLocation.searchStr || "");
      if (window.location.pathname + window.location.search !== newPath) {
        window.history.pushState({}, "", newPath);
      }
    });
  }, [router]);

  useEffect(() => {
    const handlePopState = () => {
      const currentPath = window.location.pathname + window.location.search;
      router.navigate({ to: currentPath });
    };
    window.addEventListener("popstate", handlePopState);
    return () => window.removeEventListener("popstate", handlePopState);
  }, [router]);

  const [pendingNavigation, setPendingNavigation] = useState<{ proceed: () => void } | null>(null);

  const guardedNavigateToMain = useCallback(
    (path: string) => {
      if (shouldBlockNavigation()) {
        setPendingNavigation({ proceed: () => onNavigateToMain(path) });
        return;
      }
      onNavigateToMain(path);
    },
    [onNavigateToMain]
  );

  const handleNavigate = (options: NavigateOptions) => {
    const mainRoutes = ["/", "/dashboard"];
    const targetPath = options.to?.toString() ?? "";
    if (mainRoutes.some((route) => targetPath === route || targetPath.startsWith("/dashboard"))) {
      guardedNavigateToMain(targetPath);
    } else {
      router.navigate(options);
    }
  };

  return (
    <div id="account" className="contents">
      <MainNavigationContext.Provider value={guardedNavigateToMain}>
        <AuthenticationProvider navigate={handleNavigate}>
          <RouterProvider router={router} />
        </AuthenticationProvider>
      </MainNavigationContext.Provider>
      <UnsavedChangesDialog
        isOpen={pendingNavigation !== null}
        onConfirmLeave={() => {
          pendingNavigation?.proceed();
          setPendingNavigation(null);
        }}
        onCancel={() => setPendingNavigation(null)}
        parentTrackingTitle="Navigation"
      />
    </div>
  );
}
