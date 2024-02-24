import fs from "fs";
import path from "path";
import { type Compiler, type RspackPluginInstance } from "@rspack/core";
import { ClientFilesystemRouterOptions, ClientFilesystemRouter } from "./lib/ClientFilesystemRouter";

const PLUGIN_NAME = "ClientFilesystemRouterPlugin";

const conventionFileNames = [
  "layout.tsx",
  "page.tsx",
  "loading.tsx",
  "error.tsx",
  "not-found.tsx"
];

export class ClientFilesystemRouterPlugin implements RspackPluginInstance {
  private router: ClientFilesystemRouter;
  private routePathPrefix: string;

  constructor(options?: ClientFilesystemRouterOptions) {
    this.router = new ClientFilesystemRouter(options);
    this.routePathPrefix = this.router.generateOptions.appPath + path.sep;
  }

  apply(compiler: Compiler) {
    const logger = compiler.getInfrastructureLogger(PLUGIN_NAME);

    let generateRouterFile = true;

    const generateRouter = () => {
      if (generateRouterFile) {
        generateRouterFile = false;
        const start = Date.now();
        const fileUpdated = writeFileIfChanged(this.router.generateOptions.outputPath, this.router.generate());
        const duration = Date.now() - start;
        const relativeOutputPath = path.relative(process.cwd(), this.router.generateOptions.outputPath);
        if (fileUpdated) {
          logger.info("Generated", `'${relativeOutputPath}'`, `(${duration}ms)`);
        } else {
          logger.info("No changes to", `'${relativeOutputPath}'`, `(${duration}ms)`);
        }
      }
    }

    compiler.hooks.invalid.tap(PLUGIN_NAME, (fileName) => {
      if (generateRouterFile === false && fileName != null && fileName.startsWith(this.routePathPrefix) && conventionFileNames.includes(path.basename(fileName))) {
        generateRouterFile = true;
        generateRouter();
      }
    });

    generateRouter();
  }
}

export default ClientFilesystemRouterPlugin;

/**
 * Write a file if the content has changed.
 * Return true if the file was written, false if the content was the same.
 */
function writeFileIfChanged(filePath: string, content: string) {
  try {
    // Check if the file already exists and has the same content
    if (fs.readFileSync(filePath, "utf8") === content) return false;
  } catch {
    // noop
  }

  fs.writeFileSync(filePath, content, "utf8");
  return true;
}