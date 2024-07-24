import { LifeBuoyIcon } from "lucide-react";
import { AvatarButton } from "./AvatarButton";
import { ThemeModeSelector } from "@repo/infrastructure/themeMode/ThemeModeSelector";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { LocaleSwitcher } from "@/shared/components/LocaleSwitcher";
import type { ReactNode } from "react";

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <div className="flex items-center justify-between">
      <Breadcrumbs>
        <Breadcrumb href="/admin">Home</Breadcrumb>
        {children}
      </Breadcrumbs>
      <div className="flex flex-row gap-6 items-center">
        <ThemeModeSelector />
        <Button variant="ghost" size="icon">
          <LifeBuoyIcon size={20} />
        </Button>
        <LocaleSwitcher />
        <AvatarButton />
      </div>
    </div>
  );
}
