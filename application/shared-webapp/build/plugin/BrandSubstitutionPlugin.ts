import type { RsbuildPlugin } from "@rsbuild/core";

import fs from "node:fs";
import path from "node:path";

// Post-build substitution of __PRODUCT_NAME__ in static asset files (e.g. manifest.json) that
// rsbuild copies verbatim from `public/`. Runs after the production build; dev mode serves the
// raw placeholder token. PWA install prompts use the production output, where this matters.
export function BrandSubstitutionPlugin(productName: string, files: string[]): RsbuildPlugin {
  return {
    name: "BrandSubstitutionPlugin",
    setup(api) {
      api.onAfterBuild(() => {
        const distPath = api.context.distPath;
        for (const file of files) {
          const fullPath = path.join(distPath, file);
          if (!fs.existsSync(fullPath)) continue;
          const content = fs.readFileSync(fullPath, "utf8");
          fs.writeFileSync(fullPath, content.replace(/__PRODUCT_NAME__/g, productName));
        }
      });
    }
  };
}
