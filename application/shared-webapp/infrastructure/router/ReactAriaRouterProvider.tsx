import { useRouter } from "@tanstack/react-router";
import { RouterProvider } from "react-aria-components";
import type { NavigateOptions, RegisteredRouter, RoutePaths, ToOptions, ToPathOption } from "@tanstack/react-router";
import type { AdditionalRoutes } from "./additionalRoutes";

/**
 * Additional paths that are not part of the route tree. These paths are used for external links or
 * links to other self contained systems.
 *
 * @example
 * - `https://example.com`
 * - `http://example.com`
 */
type AdditionalPathsType = `${AdditionalRoutes}${string}`;
const additionalPathPrefixes: AdditionalPathsType[] = ["https://", "http://"];

/**
 * Routes part of the route tree.
 */
type RoutePathsType = RoutePaths<RegisteredRouter["routeTree"]>;

declare module "react-aria-components" {
  interface RouterConfig {
    href: ToPathOption<RegisteredRouter, RoutePathsType, RoutePathsType | AdditionalPathsType>;
    routerOptions: Omit<NavigateOptions, keyof ToOptions>;
  }
}

export interface ReactAriaRouterProviderProps {
  children: React.ReactNode;
}

export function ReactAriaRouterProvider({ children }: Readonly<ReactAriaRouterProviderProps>) {
  const router = useRouter();

  return (
    <RouterProvider
      navigate={(to, options) => router.navigate({ to, ...options })}
      useHref={(to) => (additionalPathPrefixes.some((p) => to.startsWith(p)) ? to : router.buildLocation({ to }).href)}
    >
      {children}
    </RouterProvider>
  );
}
