import type { RsbuildConfig, RsbuildPlugin } from "@rsbuild/core";

/**
 * For now there's not a lingui plugin for rsbuild, so we need to configure the
 * swc plugin to use the lingui swc plugin.
 *
 * Note: This plugin should be removed once the lingui plugin is available.
 */

export function LinguiPlugin(): RsbuildPlugin {
  return {
    name: "LinguiPlugin",
    setup(api) {
      api.modifyRsbuildConfig((userConfig, { mergeRsbuildConfig }) => {
        const extraConfig: RsbuildConfig = {
          tools: {
            // Configure the swc plugin to use the lingui swc plugin
            swc: {
              jsc: {
                experimental: {
                  plugins: [["@lingui/swc-plugin", {}]]
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
