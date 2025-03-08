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
        "./AvatarButton": "./shared/components/AvatarButton.tsx"
      }
    })
  ]
});
