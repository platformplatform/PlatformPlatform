import { TopMenu } from "@/shared/components/topMenu";
import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { type Locale, translationContext } from "@repo/infrastructure/translations/TranslationContext";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { createFileRoute } from "@tanstack/react-router";
import SharedSideMenu from "account-management/SharedSideMenu";
import { use } from "react";

export const Route = createFileRoute("/back-office/")({
  component: Home
});

export default function Home() {
  const { i18n } = useLingui();
  const { getLocaleInfo, locales, setLocale } = use(translationContext);

  const currentLocale = i18n.locale as Locale;
  const currentLocaleLabel = getLocaleInfo(currentLocale).label;

  return (
    <>
      <SharedSideMenu
        ariaLabel={t`Toggle collapsed menu`}
        currentSystem="back-office"
        currentLocale={currentLocale}
        currentLocaleLabel={currentLocaleLabel}
        locales={locales.map((locale) => ({
          value: locale,
          label: getLocaleInfo(locale).label
        }))}
        onLocaleChange={(locale: string) => setLocale(locale as Locale)}
      />
      <AppLayout topMenu={<TopMenu />}>
        <h1>
          <Trans>Welcome to the Back Office</Trans>
        </h1>
        <p>
          <Trans>
            Manage tenants, view system data, see exceptions, and perform various tasks for operational and support
            teams.
          </Trans>
        </p>
      </AppLayout>
    </>
  );
}
