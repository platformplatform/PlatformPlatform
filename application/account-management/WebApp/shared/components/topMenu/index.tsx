import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { LocaleSwitcher } from "@repo/infrastructure/translations/LocaleSwitcher";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { LifeBuoyIcon } from "lucide-react";
import type { ReactNode } from "react";
import AvatarButton from "../AvatarButton";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <div className="flex items-center justify-between w-full">
      <Breadcrumbs>
        <Breadcrumb href="/admin">
          <Trans>Home</Trans>
        </Breadcrumb>
        {children}
      </Breadcrumbs>
      <div className="flex flex-row gap-6 items-center">
        <span className="hidden sm:flex gap-2">
          <ThemeModeSelector />
          <Button variant="icon" aria-label={t`Help`}>
            <LifeBuoyIcon size={20} />
          </Button>
          <LocaleSwitcher />
        </span>
        <AvatarButton />
      </div>
    </div>
  );
}
