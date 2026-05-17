import { loadPlatformSettings } from "@repo/build/platformSettings";
import { BrandSubstitutionPlugin } from "@repo/build/plugin/BrandSubstitutionPlugin";
import { DevelopmentServerPlugin } from "@repo/build/plugin/DevelopmentServerPlugin";
import { FileSystemRouterPlugin } from "@repo/build/plugin/FileSystemRouterPlugin";
import { LinguiPlugin } from "@repo/build/plugin/LinguiPlugin";
import { RunTimeEnvironmentPlugin } from "@repo/build/plugin/RunTimeEnvironmentPlugin";
import { TailwindPlugin } from "@repo/build/plugin/TailwindPlugin";
import { defineConfig } from "@rsbuild/core";
import { pluginReact } from "@rsbuild/plugin-react";
import { pluginSourceBuild } from "@rsbuild/plugin-source-build";
import { pluginSvgr } from "@rsbuild/plugin-svgr";
import { pluginTypeCheck } from "@rsbuild/plugin-type-check";

const customBuildEnv: CustomBuildEnv = loadPlatformSettings();

function requirePort(name: string): number {
  // In production builds, port env vars aren't relevant (no dev server). Returning 0 keeps the
  // dev-server config valid; the dev-server plugin no-ops in production anyway.
  if (process.env.NODE_ENV === "production") return 0;
  const value = process.env[name];
  if (!value) throw new Error(`${name} must be set. rsbuild must be launched via Aspire AppHost.`);
  const port = Number.parseInt(value, 10);
  if (!Number.isFinite(port) || port <= 0) throw new Error(`${name} is not a valid positive integer: '${value}'.`);
  return port;
}

const backOfficeStaticPort = requirePort("BACK_OFFICE_STATIC_PORT");

export default defineConfig({
  dev: {
    lazyCompilation: false
  },
  security: {
    nonce: "{{cspNonce}}"
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
    DevelopmentServerPlugin({ port: backOfficeStaticPort }),
    BrandSubstitutionPlugin(
      {
        __PRODUCT_NAME__: customBuildEnv.branding.productName,
        __THEME_COLOR__: customBuildEnv.branding.themeColor.light,
        __BACKGROUND_COLOR__: customBuildEnv.branding.backgroundColor
      },
      ["manifest.json"]
    )
  ]
});
