import type { LinguiConfig } from "@lingui/conf";
import { formatter } from "@lingui/format-po";
import { locales, pseudoLocale, sourceLocale } from "./translations/i18n";

const config: LinguiConfig = {
  locales,
  sourceLocale,
  pseudoLocale,
  catalogs: [
    {
      path: "<rootDir>/translations/locale/{locale}",
      include: ["<rootDir>"],
      exclude: ["**/node_modules/**"],
    },
  ],
  format: formatter({ origins: false }),
};

export default config;
