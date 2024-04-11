import { type LinguiConfig } from "@lingui/conf";
import { formatter } from "@lingui/format-po";
import { defaultLocale, locales, pseudoLocale } from "./translations/i18n";

const config: LinguiConfig = {
  locales,
  sourceLocale: defaultLocale,
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
