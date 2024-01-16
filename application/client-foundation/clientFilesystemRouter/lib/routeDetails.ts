import fs from "fs";
import path from "path";

type ParamType = "string" | "string[]" | "string?" | "string[]?";

type ParamScope = {
  [key: string]: ParamType;
};

export type RouteCommon = {
  importRootPath: string;
  params: ParamScope;
};

export type RoutePageType = "page" | "layout" | "loading" | "not-found" | "error";

export type RoutePage = {
  type: RoutePageType;
  page: string;
  loadingPage?: string;
} & RouteCommon;

export type RouteEntry = {
  type: "entry";
  pathname: string;
  segment: string;
  aliases: string[];
  children: RouteType[];
  error?: RoutePage;
  layout?: RoutePage;
} & RouteCommon;

export type RouteType = RouteEntry | RoutePage;

const entryFileMap: Record<string, RoutePageType> = {
  "page.tsx": "page",
  "layout.tsx": "layout",
  "loading.tsx": "loading",
  "not-found.tsx": "not-found",
  "error.tsx": "error",
};

const entryFiles = Object.keys(entryFileMap);

export type RouteDetailOptions = {
  appPath: string;
  importPrefix?: string;
};

export function getRouteDetails(segments: string[], { importPrefix = "@", appPath }: RouteDetailOptions): RouteEntry {
  const routePath = path.join(appPath, ...segments);
  const importRootPath = path.posix.join(importPrefix, "app", ...segments);
  const params = getParamScope(segments);
  const routeItems = fs.readdirSync(routePath).map((file) => ({
    file,
    stats: fs.statSync(path.join(routePath, file)),
  }));

  const childSegments = routeItems
    .filter(({ file, stats }) => stats.isDirectory() && !file.startsWith("_"))
    .map(({ file }) => getRouteDetails([...segments, file], { importPrefix, appPath }));
  const childPages = routeItems.filter(({ file, stats }) => stats.isFile() && entryFiles.includes(file));

  const loadingPage = childPages.find(({ file }) => entryFileMap[file] === "loading");
  const routeEntryChildren: RoutePage[] = childPages
    .filter(({ file }) => entryFileMap[file] !== "loading")
    .map(({ file }) => ({
      type: entryFileMap[file],
      page: file,
      loadingPage: entryFileMap[file] === "page" ? loadingPage?.file : undefined,
      importRootPath,
      params,
    }));

  return {
    type: "entry",
    pathname: segments.length === 1 ? "/" : segments.join("/"),
    segment: segments[segments.length - 1],
    aliases: getReactRouterSegment(segments[segments.length - 1]),
    children: [...routeEntryChildren.filter((r) => ["layout", "error"].includes(r.type) === false), ...childSegments],
    importRootPath,
    params,
    error: routeEntryChildren.find(({ type }) => type === "error"),
    layout: routeEntryChildren.find(({ type }) => type === "layout"),
  };
}

function getParamScope(routes: string[]): ParamScope {
  return routes
    .filter((r) => r.startsWith("["))
    .map((r) => getSegmentParamType(r))
    .reduce(
      (scope, { name, isArray, isOptional }) => ({
        ...scope,
        [name]: (isArray ? "string[]" : "string") + (isOptional ? "?" : ""),
      }),
      {}
    );
}

function getSegmentParamType(segment: string) {
  const isOptional = segment.startsWith("[[");
  const name = isOptional ? segment.slice(2, -2) : segment.slice(1, -1);
  const isArray = name.startsWith("...");
  return {
    name: isArray ? name.slice(3) : name,
    isOptional,
    isArray,
  };
}

function getReactRouterSegment(segment: string): string[] {
  if (segment === "") return ["/"];
  if (!segment.startsWith("[")) return [`${segment}`];
  const { name, isArray, isOptional } = getSegmentParamType(segment);
  if (isOptional) {
    return isArray ? [`:${name}*?`, ""] : [`:${name}?`, ""];
  }
  return isArray ? [`:${name}*`] : [`:${name}`];
}
