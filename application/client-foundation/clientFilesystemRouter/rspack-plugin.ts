import fs from "fs";
import path from "path";
import { type Compiler, type RspackPluginInstance } from "@rspack/core";
import { ClientFilesystemRouterOptions, ClientFilesystemRouter } from "./lib/ClientFilesystemRouter";

const PLUGIN_NAME = "ClientFilesystemRouterPlugin";

const { name } = require("./package.json");
const routerPath = path.join("react", "ReactFilesystemRouter");
const moduleIdentifier = path.join(__dirname, "react");
const absoluteGeneratedPath = path.join(__dirname, routerPath);
const GENERATED_IMPORT_PATH = path.join(name, routerPath);

const conventionFileNames = [
  "layout.tsx",
  "page.tsx",
  "loading.tsx",
  "error.tsx",
  "not-found.tsx"
];

if (!fs.existsSync(`${absoluteGeneratedPath}.d.ts`)) {
  throw new Error(`Router exported path "${routerPath}.d.ts" does not exist in "${name}".`);
}

export class ClientFilesystemRouterPlugin implements RspackPluginInstance {
  private router: ClientFilesystemRouter;
  private routePathPrefix: string;

  constructor(options?: ClientFilesystemRouterOptions) {
    this.router = new ClientFilesystemRouter(options);
    this.routePathPrefix = this.router.generateOptions.appPath + path.sep;
  }

  apply(compiler: Compiler) {
    const logger = compiler.getInfrastructureLogger(PLUGIN_NAME);

    let shouldRebuildModule = false;
    compiler.hooks.invalid.tap(PLUGIN_NAME, (fileName) => {
      if (fileName != null && fileName.startsWith(this.routePathPrefix) && conventionFileNames.includes(path.basename(fileName))) {
        // Force a rebuild of the router module
        shouldRebuildModule = true;
      }
    });

    compiler.hooks.compilation.tap(PLUGIN_NAME, (compilation) => {
      if (shouldRebuildModule) {
        // Rebuild the router module
        shouldRebuildModule = false;

        const routerModule = compilation.modules.find((m) => m.resource?.startsWith(moduleIdentifier));
        
        if (routerModule == null) {
          logger.error(`Could not find module "${moduleIdentifier}"`);
          return;
        }

        compilation.rebuildModule(routerModule, (err) => {
          if (err) {
            logger.error(`Error rebuilding "${GENERATED_IMPORT_PATH}"`, err);
          }
        });
      }
    });

    compiler.hooks.normalModuleFactory.tap(PLUGIN_NAME, (normalModuleFactory) => {
      normalModuleFactory.hooks.beforeResolve.tapPromise(PLUGIN_NAME, async (resolveData) => {
        if (resolveData.request.endsWith("/ReactFilesystemRouter")) {
          // Generate the router module
          const absoluteRequiredPath = path.resolve(resolveData.context ?? "", resolveData.request);
          if (absoluteRequiredPath === absoluteGeneratedPath) {
            const start = Date.now();
            const generatedCode = this.router.generate();
            const duration = Date.now() - start;
            logger.info("Generated", `'${GENERATED_IMPORT_PATH}'`, `(${duration}ms)`);
            resolveData.request = `data:text/javascript,${generatedCode}`;
            return true;
          }
        }
      });
    });
  }
}

export default ClientFilesystemRouterPlugin;
