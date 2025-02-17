import type { RsbuildConfig, RsbuildPlugin } from "@rsbuild/core";
import { TanStackRouterRspack } from "@tanstack/router-plugin/rspack";

/**
 * Rsbuild plugin that adds the TanStack router to the build pipeline.
 */

export function FileSystemRouterPlugin(): RsbuildPlugin {
  return {
    name: "FileSystemRouterPlugin",
    setup(api) {
      api.modifyRsbuildConfig((userConfig, { mergeRsbuildConfig }) => {
        const extraConfig: RsbuildConfig = {
          tools: {
            rspack: {
              plugins: [TanStackRouterRspack()]
            }
          }
        };
        return mergeRsbuildConfig(userConfig, extraConfig);
      });
    }
  };
}
