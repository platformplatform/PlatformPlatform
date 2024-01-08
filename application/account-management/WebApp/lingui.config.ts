import type { LinguiConfig } from "@lingui/conf";
import { formatter } from "@lingui/format-po";
import { locales, pseudoLocale, sourceLocale } from "./src/translations/i18n";

const config: LinguiConfig = {
  locales,
  sourceLocale,
  pseudoLocale,
  catalogs: [
    {
      path: "<rootDir>/src/translations/locale/{locale}",
      include: ["<rootDir>/src"],
      exclude: ["**/node_modules/**"],
    },
  ],
  format: formatter({ origins: false }),
};

export default config;
