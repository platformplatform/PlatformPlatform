import type { ReactNode } from "react";
import { HeroImage } from "@/shared/components/HeroImage";
import { LocaleSwitcher } from "@repo/infrastructure/translations/LocaleSwitcher";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { Button } from "@repo/ui/components/Button";
import { t } from "@lingui/core/macro";
import { LifeBuoyIcon } from "lucide-react";

interface HorizontalHeroLayoutProps {
  children?: ReactNode;
}

export function HorizontalHeroLayout({ children }: Readonly<HorizontalHeroLayoutProps>) {
  return (
    <main className="flex min-h-screen flex-col relative">
      <div className="absolute top-4 right-4 gap-4 p-2 rounded-md shadow-md bg-white dark:bg-gray-800 hidden sm:flex">
        <ThemeModeSelector />
        <Button variant="icon" aria-label={t`Help`}>
          <LifeBuoyIcon size={20} />
        </Button>
        <LocaleSwitcher />
      </div>
      <div className="flex grow flex-col gap-4 md:flex-row">
        <div className="flex flex-col items-center justify-center gap-6 md:w-1/2 p-6">{children}</div>
        <div className="flex items-center justify-center p-6 bg-gray-50 dark:bg-gray-900 md:w-1/2 md:px-28 md:py-12">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
