import fs from "node:fs";
import path from "node:path";
import type { RsbuildConfig, RsbuildPlugin } from "@rsbuild/core";
import { logger } from "@rsbuild/core";

/**
 * The application ID is the relative path from the root of the repository to the
 * current working directory. This is used to identify the application in the
 * Application Insights telemetry.
 *
 * @example "account-management/webapp"
 */
const APPLICATION_ID = path.relative(path.join(process.cwd(), "..", ".."), process.cwd()).toLowerCase();
const indexHtmlPath = path.join(process.cwd(), "dist", "index.html");

const defaultUserInfoEnv: UserInfoEnv = {
  isAuthenticated: false,
  locale: "en-US"
};

const defaultRuntimeEnv: RuntimeEnv = {
  PUBLIC_URL: "/",
  CDN_URL: "/",
  APPLICATION_VERSION: "1.0.0",
  LOCALE: "en-US"
};

/**
 * Plugin to set the runtime environment variables for the application.
 *
 * @param customBuildEnv - Custom build environment variables
 * @param userInfoEnv - User information environment variables (only used in development)
 * @param runtimeEnv - Runtime environment variables (only used in development)
 */
export function RunTimeEnvironmentPlugin<E extends {} = Record<string, unknown>>(
  customBuildEnv: E,
  userInfoEnv: UserInfoEnv = defaultUserInfoEnv,
  runtimeEnv: RuntimeEnv = defaultRuntimeEnv
): RsbuildPlugin {
  return {
    name: "RunTimeEnvironmentPlugin",
    setup(api) {
      api.modifyRsbuildConfig((userConfig, { mergeRsbuildConfig }) => {
        const extraConfig: RsbuildConfig = {
          source: {
            entry: {
              // Add the runtime environment file as the first entry point
              index: [path.join(__dirname, "..", "environment", "runtime.js"), "./main.tsx"]
            },
            // Define the runtime environment variables as part of import.meta.*
            // The method getApplicationEnvironment() is defined in the runtime
            // environment file loaded as the first entry point
            define: {
              "process.env.NODE_ENV": JSON.stringify(process.env.NODE_ENV),
              "import.meta.build_env": JSON.stringify({
                APPLICATION_ID,
                ...customBuildEnv
              }),
              "import.meta.runtime_env": "getApplicationEnvironment().runtimeEnv",
              "import.meta.user_info_env": "getApplicationEnvironment().userInfoEnv",
              "import.meta.env": "getApplicationEnvironment().env"
            }
          },
          output: {
            // Set publicPath to auto to enable the server to serve the files
            assetPrefix: "auto",
            // Clean the dist folder before building
            cleanDistPath: true
          },
          html: {
            // Use the template file from the public directory
            template: "./public/index.html",
            // Define the runtime environment variables as part of the template
            meta: {
              runtimeEnv: "%ENCODED_RUNTIME_ENV%",
              userInfoEnv: "%ENCODED_USER_INFO_ENV%"
            },
            // Add the CDN URL placeholder to the script and link tags in the
            // template file
            tags(tags) {
              return tags.map((tag) => {
                if (tag.tag === "script" && tag.attrs?.src) {
                  tag.attrs.src = `%CDN_URL%/${tag.attrs.src}`;
                }
                if (tag.tag === "link" && tag.attrs?.href) {
                  tag.attrs.href = `%CDN_URL%/${tag.attrs.href}`;
                }
                return tag;
              });
            }
          },
          dev: {
            setupMiddlewares: [
              (middlewares) => {
                logger.info("Setting up runtime environment middleware");
                middlewares.unshift((req, res, next) => {
                  if (["", ".html"].includes(path.extname(req.url ?? ""))) {
                    res.setHeader("Content-Type", "text/html");
                    res.end(
                      fs
                        .readFileSync(indexHtmlPath, "utf-8")
                        .replace("%ENCODED_RUNTIME_ENV%", Buffer.from(JSON.stringify(runtimeEnv)).toString("base64"))
                        .replace("%ENCODED_USER_INFO_ENV%", Buffer.from(JSON.stringify(userInfoEnv)).toString("base64"))
                        .replace(/%CDN_URL%/g, "")
                    );
                    return;
                  }

                  next();
                });
              }
            ]
          }
        };

        return mergeRsbuildConfig(userConfig, extraConfig);
      });
    }
  };
}
