import fs from "node:fs";
import path from "node:path";
import type { RsbuildConfig, RsbuildPlugin } from "@rsbuild/core";
import tailwindPostcssPlugin from "@tailwindcss/postcss";
import type postcss from "postcss";

/**
 * Path to the shared Tailwind CSS styles file.
 */
const tailwindStyleFilename = path.resolve(__dirname, "..", "..", "..", "ui", "tailwind.css");

if (fs.existsSync(tailwindStyleFilename) === false) {
  // This is a critical error, so we exit the process letting the developer know.
  console.error(`TailwindPlugin: Tailwind styles not found at expected location: ${tailwindStyleFilename}`);
  process.exit(1);
}

/**
 * Tailwind rsbuild plugin to process Tailwind CSS styles with an optional
 * prefix for scoping in a micro-frontend setup.
 *
 * @param prefix
 */
export function TailwindPlugin(prefix?: string): RsbuildPlugin {
  const scopeTailwindSelector: postcss.TransformCallback = (root: postcss.Root) => {
    const { file } = root.source?.input || {};

    if (file != null && file !== tailwindStyleFilename) {
      // Only process the main Tailwind CSS file
      return;
    }

    if (!prefix) {
      // No prefix provided, no need to modify selectors
      return;
    }

    root.walkRules((rule: postcss.Rule) => {
      if (!rule.selectors) {
        // No selectors to process
        return;
      }

      // Prefix each selector with the provided prefix
      rule.selectors = rule.selectors.map((selector, _index) =>
        selector.startsWith(".") ? `${prefix} ${selector}` : selector
      );
    });
  };

  return {
    name: "TailwindPlugin",
    setup(api) {
      api.modifyRsbuildConfig((userConfig, { mergeRsbuildConfig }) => {
        const extraConfig: RsbuildConfig = {
          tools: {
            postcss(_, { addPlugins }) {
              addPlugins([tailwindPostcssPlugin(), scopeTailwindSelector]);
            }
          }
        };
        return mergeRsbuildConfig(userConfig, extraConfig);
      });
    }
  };
}
