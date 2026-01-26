import "@repo/ui/tailwind.css";
import { PageTracker } from "@repo/infrastructure/applicationInsights/PageTracker";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { AuthSyncModal } from "@repo/infrastructure/auth/AuthSyncModal";
import { useErrorTrigger } from "@repo/infrastructure/development/useErrorTrigger";
import { createBlockableMemoryHistory } from "@repo/infrastructure/router/createBlockableMemoryHistory";
import { useInitializeLocale } from "@repo/infrastructure/translations/useInitializeLocale";
import { AddToHomescreen } from "@repo/ui/components/AddToHomescreen";
import { ThemeModeProvider } from "@repo/ui/theme/mode/ThemeMode";
import { QueryClientProvider } from "@tanstack/react-query";
import { createRouter, type NavigateOptions, RouterProvider } from "@tanstack/react-router";
import { useEffect, useMemo } from "react";
import { queryClient } from "../shared/lib/api/client";
import { routeTree } from "../shared/lib/router/routeTree.generated";
import AuthSyncModalComponent from "./common/AuthSyncModal";

export interface AccountAppProps {
  initialPath: string;
  onNavigateToMain: (path: string) => void;
}

export default function AccountApp({ initialPath, onNavigateToMain }: Readonly<AccountAppProps>) {
  const router = useMemo(() => {
    const memoryHistory = createBlockableMemoryHistory({ initialEntries: [initialPath] });
    return createRouter({
      routeTree,
      history: memoryHistory,
      defaultPreload: "intent"
    });
  }, [initialPath]);

  useInitializeLocale();
  useErrorTrigger();

  useEffect(() => {
    return router.subscribe("onResolved", ({ toLocation }) => {
      const searchString = toLocation.searchStr || "";
      const newPath = toLocation.pathname + searchString;
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

  const handleNavigate = (options: NavigateOptions) => {
    const mainRoutes = ["/", "/dashboard"];
    const targetPath = options.to?.toString() ?? "";
    if (mainRoutes.some((route) => targetPath === route || targetPath.startsWith("/dashboard"))) {
      onNavigateToMain(targetPath);
    } else {
      router.navigate(options);
    }
  };

  return (
    <div id="account" className="contents">
      <QueryClientProvider client={queryClient}>
        <ThemeModeProvider>
          <AuthenticationProvider navigate={handleNavigate}>
            <AddToHomescreen />
            <PageTracker />
            <RouterProvider router={router} />
            <AuthSyncModal modalComponent={AuthSyncModalComponent} />
          </AuthenticationProvider>
        </ThemeModeProvider>
      </QueryClientProvider>
    </div>
  );
}
