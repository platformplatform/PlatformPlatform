import { DevelopmentServerPlugin } from "@repo/build/plugin/DevelopmentServerPlugin";
import { FileSystemRouterPlugin } from "@repo/build/plugin/FileSystemRouterPlugin";
import { LinguiPlugin } from "@repo/build/plugin/LinguiPlugin";
import { ModuleFederationPlugin } from "@repo/build/plugin/ModuleFederationPlugin";
import { RunTimeEnvironmentPlugin } from "@repo/build/plugin/RunTimeEnvironmentPlugin";
import { defineConfig } from "@rsbuild/core";
import { pluginReact } from "@rsbuild/plugin-react";
import { pluginSvgr } from "@rsbuild/plugin-svgr";
import { pluginTypeCheck } from "@rsbuild/plugin-type-check";

const customBuildEnv: CustomBuildEnv = {};

export default defineConfig({
  tools: {
    rspack: {
      // Exclude tests/e2e directory from file watching to prevent hot reloading issues
      watchOptions: {
        ignored: ["**/tests/**", "**/playwright-report/**"]
      }
    }
  },
  plugins: [
    pluginReact(),
    pluginTypeCheck(),
    pluginSvgr(),
    FileSystemRouterPlugin(),
    RunTimeEnvironmentPlugin(customBuildEnv),
    LinguiPlugin(),
    DevelopmentServerPlugin({ port: 9101 }),
    ModuleFederationPlugin({
      exposes: {
        "./FederatedSideMenu": "./federated-modules/sideMenu/FederatedSideMenu.tsx",
        "./FederatedTopMenu": "./federated-modules/topMenu/FederatedTopMenu.tsx",
        "./AuthSyncModal": "./federated-modules/common/AuthSyncModal.tsx",
        "./translations/en-US": "./shared/translations/locale/en-US.ts",
        "./translations/da-DK": "./shared/translations/locale/da-DK.ts",
        "./translations/nl-NL": "./shared/translations/locale/nl-NL.ts"
      }
    })
  ]
});
