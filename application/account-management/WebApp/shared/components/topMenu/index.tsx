import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { LocaleSwitcher } from "@repo/infrastructure/translations/LocaleSwitcher";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { LifeBuoyIcon } from "lucide-react";
import type { ReactNode } from "react";
import AvatarButton from "../AvatarButton";

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <nav className="hidden w-full items-center justify-between sm:flex">
      <Breadcrumbs>
        <Breadcrumb href="/admin">
          <Trans>Home</Trans>
        </Breadcrumb>
        {children}
      </Breadcrumbs>
      <div className="flex flex-row items-center gap-6">
        <span className="flex gap-2">
          <ThemeModeSelector aria-label={t`Toggle theme`} />
          <TooltipTrigger delay={200}>
            <Button variant="icon" aria-label={t`Help`}>
              <LifeBuoyIcon size={20} />
            </Button>
            <Tooltip>{t`Support`}</Tooltip>
          </TooltipTrigger>
          <TooltipTrigger delay={200}>
            <LocaleSwitcher aria-label={t`Select language`} />
            <Tooltip>{t`Change language`}</Tooltip>
          </TooltipTrigger>
        </span>
        <AvatarButton aria-label={t`User profile menu`} />
      </div>
    </nav>
  );
}
