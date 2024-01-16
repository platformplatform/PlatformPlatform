import { createElement } from "react";
import { RouterProvider } from "react-router-dom";
import { router } from "./ReactFilesystemRouter";

export { Link, useParams, useNavigate } from "react-router-dom";

/**
 * React Router Provider serving the routes defined in the filesystem.
 *
 * @returns A React Router Provider serving the routes defined in the filesystem.
 *
 * @example
 * ```tsx
 * import { ReactFilesystemRouter } from "@platformplatform/client-filesystem-router/react";
 *
 * export const App = () => (
 *  <ReactFilesystemRouter />
 * );
 * ```
 */
export const ReactFilesystemRouter = () => createElement(RouterProvider, { router });

export type NavigateOptions = {
  replace?: boolean;
};

/**
 * Navigate to a route.
 *
 * @param to The route to navigate to.
 * @param options Options for the navigation.
 *
 * @example
 * ```tsx
 * import { navigate } from "@platformplatform/client-filesystem-router/react";
 *
 * const MyComponent = () => {
 *   const onClick = () => navigate("/my-route");
 *   return <button onClick={onClick}>Navigate</button>;
 * };
 * ```
 */
export const navigate = (to: string, options?: NavigateOptions) => router.navigate(to, options);
