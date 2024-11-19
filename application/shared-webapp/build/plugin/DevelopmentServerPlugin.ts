import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import type { RsbuildConfig, RsbuildPlugin } from "@rsbuild/core";
import { logger } from "@rsbuild/core";

/**
 * Build ignore pattern for the dist folder
 */
const applicationRoot = path.resolve(process.cwd(), "..", "..");
const distFolder = path.join(process.cwd(), "dist");
const ignoreDistPattern = `**/${path.relative(applicationRoot, distFolder)}/**`;

/**
 * Files to write to disk for the development server to serve
 */
const writeToDisk = ["index.html", "remoteEntry.js", "robots.txt", "favicon.ico"];

export type DevelopmentServerPluginOptions = {
  /**
   * The port to start the development server on
   */
  port: number;
};

/**
 * RsBuild plugin to configure the development server to use the platformplatform.pfx certificate and
 * allow CORS for the platformplatform server.
 *
 * @param options - The options for the plugin
 */
export function DevelopmentServerPlugin(options: DevelopmentServerPluginOptions): RsbuildPlugin {
  return {
    name: "DevelopmentServerPlugin",
    setup(api) {
      api.modifyRsbuildConfig((userConfig, { mergeRsbuildConfig }) => {
        if (process.env.NODE_ENV === "production") {
          // Do not modify the Rsbuild config in production
          return userConfig;
        }

        // Path to the platformplatform.pfx certificate generated as part of the Aspire setup
        const pfxPath = path.join(os.homedir(), ".aspnet", "dev-certs", "https", "localhost.pfx");
        const passphrase = process.env.CERTIFICATE_PASSWORD ?? "";

        if (!fs.existsSync(pfxPath)) {
          throw new Error(`Certificate not found at path: ${pfxPath}`);
        }

        if (passphrase === "") {
          throw new Error("CERTIFICATE_PASSWORD environment variable is not set");
        }

        logger.info(`Using ignore pattern: ${ignoreDistPattern}`);

        const extraConfig: RsbuildConfig = {
          server: {
            // If the port is in use, the server will exit with an error
            strictPort: true,
            // Allow CORS for the platformplatform server
            headers: {
              "Access-Control-Allow-Origin": "*"
            },
            // Start the server on the specified port with the platformplatform.pfx certificate
            port: options.port,
            https: {
              pfx: fs.readFileSync(pfxPath),
              passphrase
            }
          },
          dev: {
            client: {
              port: options.port
            },
            // Set publicPath to auto to enable the server to serve the files
            assetPrefix: "auto",
            // Write files to "dist" folder enabling the Api to serve them
            writeToDisk: (filename: string) => {
              return writeToDisk.some((file) => filename.endsWith(file));
            }
          },
          tools: {
            rspack: {
              watchOptions: {
                // Ignore the dist folder to prevent infinite loop as we are writing files to dist
                ignored: ignoreDistPattern
              }
            }
          }
        };

        return mergeRsbuildConfig(userConfig, extraConfig);
      });
    }
  };
}
