import { HeroImage } from "@/shared/components/HeroImage";
import { t } from "@lingui/core/macro";
import { LocaleSwitcher } from "@repo/infrastructure/translations/LocaleSwitcher";
import { Button } from "@repo/ui/components/Button";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { LifeBuoyIcon } from "lucide-react";
import type { ReactNode } from "react";

interface HorizontalHeroLayoutProps {
  children: ReactNode;
}

export function HorizontalHeroLayout({ children }: Readonly<HorizontalHeroLayoutProps>) {
  return (
    <main className="relative flex min-h-screen flex-col">
      <div className="absolute top-4 right-4 hidden gap-4 rounded-md bg-white p-2 shadow-md sm:flex dark:bg-gray-800">
        <ThemeModeSelector aria-label={t`Toggle theme`} />
        <Button variant="icon" aria-label={t`Help`}>
          <LifeBuoyIcon size={20} />
        </Button>
        <LocaleSwitcher aria-label={t`Select language`} />
      </div>
      <div className="flex grow flex-col gap-4 md:flex-row">
        <div className="flex w-full flex-col items-center justify-center gap-6 bg-background p-6 md:w-1/2">
          {children}
          {/* Mobile-only icon controls at bottom of form */}
          <div className="flex gap-4 rounded-md bg-white p-2 shadow-md sm:hidden dark:bg-gray-800">
            <ThemeModeSelector aria-label={t`Toggle theme`} />
            <Button variant="icon" aria-label={t`Help`}>
              <LifeBuoyIcon size={20} />
            </Button>
            <LocaleSwitcher aria-label={t`Select language`} />
          </div>
        </div>
        <div className="hidden items-center justify-center bg-input-background p-6 md:flex md:w-1/2 md:px-28 md:py-12">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
