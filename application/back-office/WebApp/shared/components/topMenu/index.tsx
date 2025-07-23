import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { LocaleSwitcher } from "@repo/infrastructure/translations/LocaleSwitcher";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import type { ReactNode } from "react";
import { Suspense, lazy } from "react";

const FederatedTopMenu = lazy(() => import("account-management/FederatedTopMenu"));
const SupportButton = lazy(() => import("account-management/SupportButton"));

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <Suspense fallback={<div className="h-12 w-full" />}>
      <FederatedTopMenu
        rightContent={
          <span className="flex gap-2">
            <ThemeModeSelector aria-label={t`Change theme`} tooltip={t`Change theme`} />
            <Suspense fallback={<Button variant="icon" isDisabled={true} />}>
              <SupportButton aria-label={t`Contact support`} />
            </Suspense>
            <LocaleSwitcher aria-label={t`Change language`} tooltip={t`Change language`} />
          </span>
        }
      >
        <Breadcrumbs>
          <Breadcrumb>
            <Trans>Home</Trans>
          </Breadcrumb>
          {children}
        </Breadcrumbs>
      </FederatedTopMenu>
    </Suspense>
  );
}
