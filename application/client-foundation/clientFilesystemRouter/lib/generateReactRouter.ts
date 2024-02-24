import { RoutePage, RouteType, getRouteDetails } from "./routeDetails";
import {
  createBrowserRouterCode,
  errorPageCode,
  layoutPageCode,
  lazyLoadingPageCode,
  normalPageCode,
} from "./pageReactRoutesCode";

type TemplateObject = Record<string, string>;

function generateReactRouterCode(routeItem: RouteType, indent = "  "): string {
  if (routeItem.type === "page") {
    if (routeItem.loadingPage) {
      const landingRoutePage: RoutePage = { ...routeItem, page: routeItem.loadingPage };
      return "\n" + [
        "{",
        "  index: true,",
        `  element: ${lazyLoadingPageCode(routeItem, landingRoutePage)},`,
        "}"
      ].map(l => indent + l).join("\n");
    }
    return "\n" + [
      `{`,
      `  index: true,`,
      `  element: ${normalPageCode(routeItem)},`,
      `}`
    ].map(l => indent + l).join("\n");
  }
  if (routeItem.type === "not-found") {
    return "\n" + [
      `{`,
      `  path: "*",`,
      `  element: ${normalPageCode(routeItem)},`,
      `}`
    ].map(l => indent + l).join("\n");
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
      result.children = `[${routeItem.children.map((c) => generateReactRouterCode(c, indent + "    ")).join(",\n")}\n  ${indent}]`;
    }

    return routeItem.aliases
      .map((alias) =>
        serializeTemplateObject({
          path: `"${alias}"`,
          ...result,
        }, indent),
      )
      .map(l => `\n${indent}${l}`)
      .join(",\n");
  }

  throw new Error(`Unhandled route type "${routeItem.type}".`);
}

export type GenerateReactRouterOptions = {
  appPath: string;
  assetPrefix?: string;
  importPrefix: string;
  outputPath: string;
};

export function generateReactRouter({ appPath, importPrefix, assetPrefix }: GenerateReactRouterOptions) {
  return createBrowserRouterCode(
    generateReactRouterCode(getRouteDetails([""], { appPath, importPrefix })),
    assetPrefix,
  );
}

function serializeTemplateObject(object: TemplateObject, indent = "") {
  return Object.entries(object).reduce((result, [key, value]) => `${result}${indent}  ${key}: ${value},\n`, `{\n`) + `${indent}}`;
}
