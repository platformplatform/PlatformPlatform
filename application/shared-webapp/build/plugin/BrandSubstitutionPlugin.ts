import type { RsbuildPlugin } from "@rsbuild/core";

import fs from "node:fs";
import path from "node:path";

// Substitution of brand tokens (e.g. __PRODUCT_NAME__, __THEME_COLOR__) in static asset files
// that rsbuild copies verbatim from `public/` -- typically manifest.json. Two code paths so dev
// and prod stay in lockstep: production rewrites the file in `dist/` after build; dev intercepts
// requests for the file and serves the substituted bytes (otherwise Chrome's manifest validator
// complains about raw color tokens, and PWA install prompts in dev show wrong values).
export function BrandSubstitutionPlugin(substitutions: Record<string, string>, files: string[]): RsbuildPlugin {
  return {
    name: "BrandSubstitutionPlugin",
    setup(api) {
      api.onAfterBuild(() => {
        const distPath = api.context.distPath;
        for (const file of files) {
          const fullPath = path.join(distPath, file);
          if (!fs.existsSync(fullPath)) continue;
          fs.writeFileSync(fullPath, applySubstitutions(fs.readFileSync(fullPath, "utf8"), substitutions));
        }
      });

      api.modifyRsbuildConfig((config) => {
        const server = (config.server ??= {});
        const existing = toArray(server.setup);
        server.setup = [
          ...existing,
          (context) => {
            if (context.action !== "dev") return;
            const publicDir = path.join(api.context.rootPath, "public");
            context.server.middlewares.use((req, res, next) => {
              const requested = (req.url ?? "").replace(/^\//, "").split("?")[0];
              if (!files.includes(requested)) {
                next();
                return;
              }
              const sourcePath = path.join(publicDir, requested);
              if (!fs.existsSync(sourcePath)) {
                next();
                return;
              }
              res.setHeader("Content-Type", contentTypeFor(requested));
              res.end(applySubstitutions(fs.readFileSync(sourcePath, "utf8"), substitutions));
            });
          }
        ];
        return config;
      });
    }
  };
}

function toArray<T>(value: T | T[] | undefined): T[] {
  if (value === undefined) return [];
  return Array.isArray(value) ? value : [value];
}

function applySubstitutions(content: string, substitutions: Record<string, string>): string {
  for (const [token, value] of Object.entries(substitutions)) {
    content = content.split(token).join(value);
  }
  return content;
}

function contentTypeFor(file: string): string {
  if (file.endsWith(".json")) return "application/json; charset=utf-8";
  if (file.endsWith(".webmanifest")) return "application/manifest+json; charset=utf-8";
  return "text/plain; charset=utf-8";
}
