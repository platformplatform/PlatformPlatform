import { t } from "@lingui/core/macro";
import { enhancedFetch } from "@repo/infrastructure/http/httpClient";
import { Button } from "@repo/ui/components/Button";
import { LogOutIcon } from "lucide-react";
import type { ReactNode } from "react";
import LocaleSwitcher from "@/federated-modules/common/LocaleSwitcher";
import SupportButton from "@/federated-modules/common/SupportButton";
import ThemeModeSelector from "@/federated-modules/common/ThemeModeSelector";
import { HeroImage } from "@/shared/components/HeroImage";

interface HorizontalHeroLayoutProps {
  children: ReactNode;
}

export function HorizontalHeroLayout({ children }: Readonly<HorizontalHeroLayoutProps>) {
  const { isAuthenticated } = import.meta.user_info_env;

  const handleLogout = async () => {
    try {
      await enhancedFetch("/api/account/authentication/logout", { method: "POST" });
    } finally {
      window.location.href = "/login";
    }
  };

  return (
    <main className="relative flex min-h-screen flex-col">
      <div className="absolute top-4 right-4 hidden gap-4 rounded-md bg-white p-2 shadow-md lg:flex dark:bg-sidebar">
        {isAuthenticated && (
          <Button variant="ghost" size="icon" onClick={handleLogout} aria-label={t`Log out`}>
            <LogOutIcon className="size-5" />
          </Button>
        )}
        <ThemeModeSelector />
        <SupportButton aria-label={t`Contact support`} />
        <LocaleSwitcher />
      </div>
      <div className="flex grow flex-col gap-4 lg:flex-row">
        <div className="flex min-h-screen w-full flex-col bg-background p-6 lg:min-h-0 lg:w-1/2">
          <div style={{ flex: 1 }} className="flex flex-col items-center justify-center gap-6">
            {children}
            {/* Mobile-only icon controls at bottom of form */}
            <div className="flex gap-4 rounded-md bg-white p-2 shadow-md lg:hidden dark:bg-sidebar">
              {isAuthenticated && (
                <Button variant="ghost" size="icon" onClick={handleLogout} aria-label={t`Log out`}>
                  <LogOutIcon className="size-5" />
                </Button>
              )}
              <ThemeModeSelector />
              <SupportButton aria-label={t`Contact support`} />
              <LocaleSwitcher />
            </div>
          </div>
        </div>
        <div className="hidden items-center justify-center bg-input-background p-6 lg:flex lg:w-1/2 lg:px-28 lg:py-12">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
