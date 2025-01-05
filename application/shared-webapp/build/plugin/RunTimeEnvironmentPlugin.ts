import path from "node:path";
import type { RsbuildConfig, RsbuildPlugin } from "@rsbuild/core";

/**
 * The application ID is the relative path from the root of the repository to the
 * current working directory. This is used to identify the application in the
 * Application Insights telemetry.
 *
 * @example "account-management/webapp"
 */
const APPLICATION_ID = path.relative(path.join(process.cwd(), "..", ".."), process.cwd()).toLowerCase();
const BUILD_TYPE = process.env.NODE_ENV === "production" ? "production" : "development";

export function RunTimeEnvironmentPlugin<E extends {} = Record<string, unknown>>(customBuildEnv: E): RsbuildPlugin {
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
              "import.meta.build_env": JSON.stringify({
                APPLICATION_ID,
                BUILD_TYPE,
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
              userInfoEnv: "%ENCODED_USER_INFO_ENV%",
              antiforgeryToken: "%ANTIFORGERY_TOKEN%"
            },
            // Add the CDN URL placeholder to the script and link tags in the template file
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
          }
        };

        return mergeRsbuildConfig(userConfig, extraConfig);
      });
    }
  };
}
