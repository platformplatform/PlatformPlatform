import type { LinguiConfig } from "@lingui/conf";
import { formatter } from "@lingui/format-po";
import { locales, pseudoLocale, sourceLocale } from "./shared/translations/i18n";

const config: LinguiConfig = {
  locales,
  sourceLocale,
  pseudoLocale,
  catalogs: [
    {
      path: "<rootDir>/shared/translations/locale/{locale}",
      include: ["<rootDir>"],
      exclude: ["**/node_modules/**", "**/dist", "**/*.d.ts", "**/*.test.*", "**/.*"]
    }
  ],
  format: formatter({ origins: false })
};

export default config;
