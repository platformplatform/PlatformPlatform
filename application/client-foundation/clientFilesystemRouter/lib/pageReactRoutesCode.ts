import { RoutePage } from "./routeDetails";

const createElementCode = (routePage: RoutePage, props: string = "{}", children: string = "null") =>
  `createElement(${requireCode(routePage)}, ${props}, ${children})`;

const requireCode = ({ importRootPath, page }: RoutePage) => `require("${importRootPath}/${page}").default`;

const dynamicImportCode = ({ importRootPath, page }: RoutePage) => `import("${importRootPath}/${page}")`;

const createLazyElementCode = (routePage: RoutePage, props: string = "{}", children: string = "null") =>
  `createElement(lazy(() => ${dynamicImportCode(routePage)}), \n// @ts-ignore\n ${props}, ${children})`;

export const layoutPageCode = (routePage: RoutePage) => `
createElement(() => {
  /* Layout */
  const children = useOutlet();
  const params = useParams();
  return ${createElementCode(routePage, "{ params }", "children")}
})
`;

export const lazyLoadingPageCode = (routePage: RoutePage, loadingPage: RoutePage) => `
createElement(() => {
  /* Lazy loading */
  const params = useParams();
  return createElement(Suspense, {
    fallback: ${createElementCode(loadingPage, "{params}")}
  }, ${createLazyElementCode(routePage, "{params}")}
  )
})
`;

export const normalPageCode = (routePage: RoutePage) => `
createElement(() => {
  /* Normal page */
  const params = useParams();
  return ${createElementCode(routePage, "{ params }")};
})
`;

export const errorPageCode = (routePage: RoutePage) => `
  createElement(() => {
    /* Error page */
    const error = useRouteError();
    const reset = () => {
      console.log("Not implemented");
    };
    return ${createElementCode(routePage, "{ error, reset }")};
  })
`;

export const createBrowserRouterCode = (routerCode: string, basename: string = "") => `
  import { createElement, Suspense, lazy } from "react";
  import { useOutlet, useParams, useRouteError, createBrowserRouter } from "react-router-dom";
  const routes = [${routerCode}];
  export const router = createBrowserRouter(routes, { basename: "${basename}" });
`;
