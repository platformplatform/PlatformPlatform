import { useRouter } from "@tanstack/react-router";
import { RouterProvider } from "react-aria-components";
import type { NavigateOptions, RegisteredRouter, RoutePaths, ToOptions, ToPathOption } from "@tanstack/react-router";
import type { RouterType } from "./router";

// Router Types
type RoutePathsType = RoutePaths<RegisteredRouter["routeTree"]>;
type RouterHrefType = RoutePathsType | `https://${string}` | `http://${string}`;
type RouterPathOptions = ToPathOption<RouterType, RoutePathsType, RouterHrefType>;
type RouterNavigateOptions = Omit<NavigateOptions, keyof ToOptions>;

declare module "react-aria-components" {
  interface RouterConfig {
    href: RouterPathOptions;
    routerOptions: RouterNavigateOptions;
  }
}

interface ReactAriaRouterProviderProps {
  children: React.ReactNode;
}

export function ReactAriaRouterProvider({ children }: Readonly<ReactAriaRouterProviderProps>) {
  const router = useRouter();

  return (
    <RouterProvider
      navigate={(to, options) => router.navigate({ to, ...options })}
      useHref={to => /^https?:/.test(to) ? to : router.buildLocation({ to }).href}
    >
      {children}
    </RouterProvider>
  );
}
