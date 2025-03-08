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
        <div className="flex flex-col items-center justify-center gap-6 p-6 md:w-1/2">{children}</div>
        <div className="flex items-center justify-center bg-gray-50 p-6 md:w-1/2 md:px-28 md:py-12 dark:bg-gray-900">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
