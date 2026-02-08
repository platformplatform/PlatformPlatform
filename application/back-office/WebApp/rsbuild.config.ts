import { DevelopmentServerPlugin } from "@repo/build/plugin/DevelopmentServerPlugin";
import { FileSystemRouterPlugin } from "@repo/build/plugin/FileSystemRouterPlugin";
import { LinguiPlugin } from "@repo/build/plugin/LinguiPlugin";
import { ModuleFederationPlugin } from "@repo/build/plugin/ModuleFederationPlugin";
import { RunTimeEnvironmentPlugin } from "@repo/build/plugin/RunTimeEnvironmentPlugin";
import { TailwindPlugin } from "@repo/build/plugin/TailwindPlugin";
import { defineConfig } from "@rsbuild/core";
import { pluginReact } from "@rsbuild/plugin-react";
import { pluginSourceBuild } from "@rsbuild/plugin-source-build";
import { pluginSvgr } from "@rsbuild/plugin-svgr";
import { pluginTypeCheck } from "@rsbuild/plugin-type-check";

const customBuildEnv: CustomBuildEnv = {};

export default defineConfig({
  dev: {
    lazyCompilation: false
  },
  security: {
    nonce: "{{cspNonce}}"
  },
  tools: {
    rspack: {
      // Exclude tests/e2e directory from file watching to prevent hot reloading issues
      watchOptions: {
        ignored: ["**/tests/**", "**/playwright-report/**"],
        // Watch workspace packages for changes
        followSymlinks: true
      },
      snapshot: {
        // Include workspace packages in module snapshots for change detection
        managedPaths: []
      }
    }
  },
  plugins: [
    TailwindPlugin("#back-office"),
    pluginReact(),
    pluginTypeCheck(),
    pluginSvgr(),
    pluginSourceBuild({
      sourceField: "source"
    }),
    FileSystemRouterPlugin(),
    RunTimeEnvironmentPlugin(customBuildEnv),
    LinguiPlugin(),
    DevelopmentServerPlugin({ port: 9201 }),
    ModuleFederationPlugin({
      remotes: {
        "account-management": { port: 9101 }
      }
    })
  ]
});
