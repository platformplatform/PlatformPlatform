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
  output: {
    copy: [
      { from: "./routes/legal/terms.en-US.md", to: "./legal/documents/terms.md" },
      { from: "./routes/legal/privacy.en-US.md", to: "./legal/documents/privacy.md" },
      { from: "./routes/legal/dpa.en-US.md", to: "./legal/documents/dpa.md" }
    ]
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
    TailwindPlugin("#account"),
    pluginReact(),
    pluginTypeCheck(),
    pluginSvgr(),
    pluginSourceBuild({
      sourceField: "source"
    }),
    FileSystemRouterPlugin(),
    RunTimeEnvironmentPlugin(customBuildEnv, { federationOnly: true }),
    LinguiPlugin(),
    DevelopmentServerPlugin({ port: 9101 }),
    ModuleFederationPlugin({
      exposes: {
        "./AccessDeniedPage": "./federated-modules/errorPages/AccessDeniedPage.tsx",
        "./AccountApp": "./federated-modules/AccountApp.tsx",
        "./AuthSyncModal": "./federated-modules/common/AuthSyncModal.tsx",
        "./Banners": "./federated-modules/banners/Banners.tsx",
        "./ErrorPage": "./federated-modules/errorPages/ErrorPage.tsx",
        "./MobileMenu": "./federated-modules/sideMenu/MobileMenu.tsx",
        "./NotFoundPage": "./federated-modules/errorPages/NotFoundPage.tsx",
        "./PublicFooter": "./federated-modules/public/PublicFooter.tsx",
        "./PublicNavigation": "./federated-modules/public/PublicNavigation.tsx",
        "./SuspendedPage": "./federated-modules/subscription/SuspendedPage.tsx",
        "./TenantStateGuard": "./federated-modules/subscription/TenantStateGuard.tsx",
        "./UserMenu": "./federated-modules/userMenu/UserMenu.tsx",
        "./translations/da-DK": "./shared/translations/locale/da-DK.ts",
        "./translations/en-US": "./shared/translations/locale/en-US.ts"
      }
    })
  ]
});
