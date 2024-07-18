import { LifeBuoyIcon } from "lucide-react";
import { UserAvatarButton } from "./UserAvatarButton";
import { ThemeModeSelector } from "@repo/infrastructure/themeMode/ThemeModeSelector";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { LocaleSwitcher } from "@/shared/ui/LocaleSwitcher";

export function TopMenu() {
  return (
    <div className="flex items-center justify-between">
      <Breadcrumbs>
        <Breadcrumb href="/admin">Home</Breadcrumb>
        <Breadcrumb href="/admin/users">Users</Breadcrumb>
        <Breadcrumb>All Users</Breadcrumb>
      </Breadcrumbs>
      <div className="flex flex-row gap-6 items-center">
        <ThemeModeSelector />
        <Button variant="ghost" size="icon">
          <LifeBuoyIcon size={20} />
        </Button>
        <LocaleSwitcher />
        <UserAvatarButton />
      </div>
    </div>
  );
}
