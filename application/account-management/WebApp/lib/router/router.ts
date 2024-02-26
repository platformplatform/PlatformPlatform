import { createElement } from "react";
import { RouterProvider } from "react-router-dom";
import { router } from "./router.generated";

export { Link, useParams, useNavigate } from "react-router-dom";

/**
 * React Router Provider serving the routes defined in the filesystem.
 *
 * @returns A React Router Provider serving the routes defined in the filesystem.
 *
 * @example
 * ```tsx
 * import { ReactFilesystemRouter } from "@/lib/router/router";
 *
 * export const App = () => (
 *  <ReactFilesystemRouter />
 * );
 * ```
 */
export const ReactFilesystemRouter = () => createElement(RouterProvider, { router });

export interface NavigateOptions {
  replace?: boolean;
}

/**
 * Navigate to a route.
 *
 * @param to The route to navigate to.
 * @param options Options for the navigation.
 *
 * @example
 * ```tsx
 * import { navigate } from "@/lib/router/router";
 *
 * const MyComponent = () => {
 *   const onClick = () => navigate("/my-route");
 *   return <button onClick={onClick}>Navigate</button>;
 * };
 * ```
 */
export const navigate = (to: string, options?: NavigateOptions) => router.navigate(to, options);
