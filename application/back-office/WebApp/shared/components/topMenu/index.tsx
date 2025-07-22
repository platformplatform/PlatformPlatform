import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { LocaleSwitcher } from "@repo/infrastructure/translations/LocaleSwitcher";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import type { ReactNode } from "react";
import { Suspense, lazy } from "react";

const AvatarButton = lazy(() => import("account-management/AvatarButton"));
const SupportButton = lazy(() => import("account-management/SupportButton"));

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <nav className="flex w-full items-center justify-between">
      <Breadcrumbs>
        <Breadcrumb>
          <Trans>Home</Trans>
        </Breadcrumb>
        {children}
      </Breadcrumbs>
      <div className="flex flex-row items-center gap-6">
        <span className="flex gap-2">
          <ThemeModeSelector aria-label={t`Change theme`} tooltip={t`Change theme`} />
          <Suspense fallback={<Button variant="icon" isDisabled={true} />}>
            <SupportButton aria-label={t`Contact support`} />
          </Suspense>
          <LocaleSwitcher aria-label={t`Change language`} tooltip={t`Change language`} />
        </span>
        <Suspense fallback={<div className="h-10 w-10 rounded-full bg-secondary" />}>
          <AvatarButton aria-label={t`User profile menu`} />
        </Suspense>
      </div>
    </nav>
  );
}
