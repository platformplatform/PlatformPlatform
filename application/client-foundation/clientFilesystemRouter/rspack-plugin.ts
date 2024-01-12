import fs from "fs";
import path from "path";
import { type Compiler, type RspackPluginInstance } from "@rspack/core";
import { ClientFilesystemRouterOptions, ClientFilesystemRouter } from "./lib/ClientFilesystemRouter";

const PLUGIN_NAME = "ClientFilesystemRouterPlugin";

const { name } = require("./package.json");
const routerPath = path.join("react", "ReactFilesystemRouter");
const GENERATED_IMPORT_PATH = path.join(name, routerPath);

const absoluteGeneratedPath = path.join(__dirname, routerPath);

if (!fs.existsSync(`${absoluteGeneratedPath}.d.ts`)) {
  throw new Error(`Router exported path "${routerPath}.d.ts" does not exist in "${name}".`);
}

export class ClientFilesystemRouterPlugin implements RspackPluginInstance {
  private router: ClientFilesystemRouter;
  constructor(options?: ClientFilesystemRouterOptions) {
    this.router = new ClientFilesystemRouter(options);
  }
  apply(compiler: Compiler) {
    const logger = compiler.getInfrastructureLogger(PLUGIN_NAME);
    compiler.hooks.normalModuleFactory.tap(PLUGIN_NAME, (normalModuleFactory) => {
      normalModuleFactory.hooks.beforeResolve.tapPromise(PLUGIN_NAME, async (resolveData) => {
        if (resolveData.request.endsWith("/ReactFilesystemRouter")) {
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
