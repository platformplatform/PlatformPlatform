import type { RsbuildConfig, RsbuildPlugin } from "@rsbuild/core";

import { logger } from "@rsbuild/core";
import fs from "node:fs";
import path from "node:path";

const manifestFile = "remoteEntry.js";
const applicationRoot = path.resolve(process.cwd(), "..", "..");
const systemRoot = path.resolve(process.cwd(), "..");
const applicationPackageJson = path.join(applicationRoot, "package.json");
const moduleFederationTypesPath = path.join(applicationRoot, "shared-webapp", "build", "module-federation-types");

if (!fs.existsSync(applicationPackageJson)) {
  throw new Error(`Cannot find package.json in the application root: ${applicationRoot}`);
}

const { dependencies } = JSON.parse(fs.readFileSync(applicationPackageJson, "utf-8"));

const SYSTEM_ID = getSystemId();

type ModuleFederationPluginOptions = {
  exposes?: Record<`./${string}`, `./${string}.tsx` | `./${string}.ts` | `./${string}`>;
  remotes?: Record<string, { port: number }>;
};

export function ModuleFederationPlugin({
  exposes = {},
  remotes = {}
}: ModuleFederationPluginOptions = {}): RsbuildPlugin {
  generateModuleFederationTypesFolder(SYSTEM_ID, exposes);
  return {
    name: "ModuleFederationPlugin",
    setup(api) {
      api.modifyRsbuildConfig((userConfig, { mergeRsbuildConfig }) => {
        const extraConfig: RsbuildConfig = {
          moduleFederation: {
            options: {
              name: snakeCase(SYSTEM_ID),
              exposes,
              remotes: getAllRemotes(SYSTEM_ID, remotes),
              filename: manifestFile,
              shared: {
                ...dependencies,
                // `react`, `react-dom`, `@lingui/core` and `@lingui/react` are EAGER singletons: they are
                // placed in the shared scope during the host's synchronous startup, before any remote's
                // federated module consumes them. Without eager, a remote loaded early (e.g. account's
                // AuthSyncModal on a direct page load) falls back to its own bundled copy, producing a second
                // `@lingui` `i18n`/`LinguiContext` — which makes `useLingui` in a federated
                // `withSystemTranslations` gate throw "used without I18nProvider". Lingui's eager modules
                // synchronously require react, so react/react-dom must be eager too.
                react: {
                  singleton: true,
                  eager: true,
                  requiredVersion: dependencies.react
                },
                "react-dom": {
                  singleton: true,
                  eager: true,
                  requiredVersion: dependencies["react-dom"]
                },
                "@lingui/core": {
                  singleton: true,
                  eager: true,
                  requiredVersion: dependencies["@lingui/core"]
                },
                "@lingui/react": {
                  singleton: true,
                  eager: true,
                  requiredVersion: dependencies["@lingui/react"]
                },
                // Router instances, route objects, and router context cross the federation boundary, so every
                // remote must bind the exact same router module instance.
                "@tanstack/react-router": {
                  singleton: true,
                  requiredVersion: dependencies["@tanstack/react-router"]
                },
                // Federated components resolve their QueryClient from the host's provider; the context object
                // doing that lookup must come from a single module instance.
                "@tanstack/react-query": {
                  singleton: true,
                  requiredVersion: dependencies["@tanstack/react-query"]
                }
              }
            }
          }
        };
        return mergeRsbuildConfig(userConfig, extraConfig);
      });
    }
  };
}

function getSystemId() {
  return path.relative(applicationRoot, systemRoot);
}

/**
 * Get all the system ids in the application root. This is used to determine the
 * module federation remotes. We also make sure that the system has a WebApp
 * and is formatted as snake_case for the module federation name.
 */
function getEverySystem() {
  return fs
    .readdirSync(applicationRoot)
    .filter((system) => fs.statSync(path.join(applicationRoot, system)).isDirectory())
    .filter((system) => fs.existsSync(path.join(applicationRoot, system, "WebApp")))
    .toSorted((a, b) => a.localeCompare(b));
}

function snakeCase(str: string) {
  return str.toLowerCase().replace(/-/g, "_");
}

function getAllRemotes(currentSystem: string, remotes: Record<string, { port: number }>) {
  const result: Record<string, string> = {};
  const everySystem = getEverySystem();
  for (const [system] of Object.entries(remotes)) {
    if (system === currentSystem) {
      throw new Error(`Cannot add self as remote: ${system}`);
    }
    if (!everySystem.includes(system)) {
      throw new Error(`Cannot find system: ${system}`);
    }

    // Main SCS is the root, so its remoteEntry.js is at /remoteEntry.js
    // Other SCSs have their remoteEntry.js at /{system}/remoteEntry.js
    const remotePath = system === "main" ? `/${manifestFile}` : `/${system}/${manifestFile}`;
    result[system] = `${snakeCase(system)}@${remotePath}`;

    logger.info(`[Module Federation] Remote: "${system}" => ${result[system]}`);
  }
  return result;
}

function generateModuleFederationTypesFolder(system: string, exposes: Record<string, string> = {}) {
  const systemTypesDefinitionPath = getTypeDefinitionPath(system);
  if (Object.keys(exposes).length === 0) {
    if (fs.existsSync(systemTypesDefinitionPath)) {
      fs.unlinkSync(systemTypesDefinitionPath);
    }
    return;
  }
  const indexContent = Object.entries(exposes)
    .map(([exportPath, importPath]) => {
      logger.info(`[Module Federation] Expose: ${exportPath} => ${importPath}`);

      const moduleName = exportPath.replace(/^\./, system);

      // The routes expose is a route-tree factory contributing routes to the host router.
      if (exportPath === "./routes") {
        return `declare module "${moduleName}" {\n  export default function createRouteTree(getParentRoute: () => unknown): unknown;\n}`;
      }

      // Every other exposed federated module is a React component.
      return `declare module "${moduleName}" {\n  export default ReactNode;\n}`;
    })
    .join("\n");

  if (!fs.existsSync(moduleFederationTypesPath)) {
    fs.mkdirSync(moduleFederationTypesPath);
  }

  fs.writeFileSync(
    systemTypesDefinitionPath,
    `// This file was auto-generated by the ModuleFederationPlugin\n\n${indexContent.trim()}\n`,
    "utf-8"
  );
}

function getTypeDefinitionPath(system: string) {
  return path.join(moduleFederationTypesPath, `${system}.d.ts`);
}
