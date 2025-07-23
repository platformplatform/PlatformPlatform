import { AvatarButton } from "@/federated-modules/topMenu/FederatedTopMenu";
import SupportButton from "@/federated-modules/support/SupportButton";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { LocaleSwitcher } from "@repo/infrastructure/translations/LocaleSwitcher";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import type { ReactNode } from "react";

interface TopMenuProps {
  children?: ReactNode;
  sidePaneOpen?: boolean;
}

export function TopMenu({ children, sidePaneOpen = false }: Readonly<TopMenuProps>) {
  return (
    <nav className="flex w-full items-center justify-between">
      <Breadcrumbs>
        <Breadcrumb href="/admin">
          <Trans>Home</Trans>
        </Breadcrumb>
        {children}
      </Breadcrumbs>
      <div
        className={`flex flex-row items-center gap-6 transition-transform duration-100 ease-in-out ${
          sidePaneOpen ? "sm:-translate-x-96 sm:transform" : ""
        }`}
      >
        <span className="flex gap-2">
          <ThemeModeSelector aria-label={t`Change theme`} tooltip={t`Change theme`} />
          <SupportButton aria-label={t`Contact support`} />
          <LocaleSwitcher aria-label={t`Change language`} tooltip={t`Change language`} />
        </span>
        <AvatarButton aria-label={t`User profile menu`} />
      </div>
    </nav>
  );
}
