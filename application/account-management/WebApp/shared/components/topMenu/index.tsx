import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { LocaleSwitcher } from "@repo/infrastructure/translations/LocaleSwitcher";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { LifeBuoyIcon } from "lucide-react";
import type { ReactNode } from "react";
import { AvatarButton } from "./AvatarButton";

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <div className="flex items-center justify-between w-full">
      <Breadcrumbs>
        <Breadcrumb href="/admin">Home</Breadcrumb>
        {children}
      </Breadcrumbs>
      <div className="flex flex-row gap-6 items-center">
        <span className="hidden sm:flex">
          <ThemeModeSelector />
          <Button variant="icon">
            <LifeBuoyIcon size={20} />
          </Button>
          <LocaleSwitcher />
        </span>
        <AvatarButton />
      </div>
    </div>
  );
}
