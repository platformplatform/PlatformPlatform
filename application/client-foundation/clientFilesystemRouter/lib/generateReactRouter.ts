import { RoutePage, RouteType, getRouteDetails } from "./routeDetails";
import {
  createBrowserRouterCode,
  errorPageCode,
  layoutPageCode,
  lazyLoadingPageCode,
  normalPageCode,
} from "./pageReactRoutesCode";

type TemplateObject = Record<string, string>;

function generateReactRouterCode(routeItem: RouteType): string {
  if (routeItem.type === "page") {
    if (routeItem.loadingPage) {
      const landingRoutePage: RoutePage = { ...routeItem, page: routeItem.loadingPage };
      return `{
        index: true,
        element: ${lazyLoadingPageCode(routeItem, landingRoutePage)},
      }`;
    }
    return `{
      index: true,
      element: ${normalPageCode(routeItem)},
    }`;
  }
  if (routeItem.type === "not-found") {
    return `{
      path: "*",
      element: ${normalPageCode(routeItem)},
    }`;
  }

  if (routeItem.type === "entry") {
    const result: TemplateObject = {};

    if (routeItem.layout != null) {
      result.element = layoutPageCode(routeItem.layout);
    }

    if (routeItem.error != null) {
      result.errorElement = errorPageCode(routeItem.error);
    }

    if (routeItem.children.length > 0) {
      result.children = `[${routeItem.children.map((c) => generateReactRouterCode(c)).join(",\n")}]`;
    }

    return routeItem.aliases
      .map((alias) =>
        serializeTemplateObject({
          path: `"${alias}"`,
          ...result,
        }),
      )
      .join(",\n");
  }

  throw new Error(`Unhandled route type "${routeItem.type}".`);
}

export type GenerateReactRouterOptions = {
  appPath: string;
  assetPrefix?: string;
  importPrefix: string;
};

export function generateReactRouter({ appPath, importPrefix, assetPrefix }: GenerateReactRouterOptions) {
  return createBrowserRouterCode(
    generateReactRouterCode(getRouteDetails([""], { appPath, importPrefix })),
    assetPrefix,
  );
}

function serializeTemplateObject(object: TemplateObject) {
  return Object.entries(object).reduce((result, [key, value]) => `${result}${key}: ${value},\n`, "{\n") + "}";
}
