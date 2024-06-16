import { defineConfig } from "@rsbuild/core";
import { pluginReact } from "@rsbuild/plugin-react";
import { pluginTypeCheck } from "@rsbuild/plugin-type-check";
import { pluginSvgr } from "@rsbuild/plugin-svgr";
import { DevelopmentServerPlugin } from "@repo/build/plugin/DevelopmentServerPlugin";
import { LinguiPlugin } from "@repo/build/plugin/LinguiPlugin";
import { RunTimeEnvironmentPlugin } from "@repo/build/plugin/RunTimeEnvironmentPlugin";

const buildEnv: BuildEnv = {};

export default defineConfig({
  plugins: [pluginReact(), pluginTypeCheck(), pluginSvgr(), RunTimeEnvironmentPlugin(buildEnv), LinguiPlugin(), DevelopmentServerPlugin({ port: 9101 })],
});