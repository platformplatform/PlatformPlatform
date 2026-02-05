import { t } from "@lingui/core/macro";
import type { ReactNode } from "react";
import LocaleSwitcher from "@/federated-modules/common/LocaleSwitcher";
import SupportButton from "@/federated-modules/common/SupportButton";
import ThemeModeSelector from "@/federated-modules/common/ThemeModeSelector";
import { HeroImage } from "@/shared/components/HeroImage";

interface HorizontalHeroLayoutProps {
  children: ReactNode;
}

export function HorizontalHeroLayout({ children }: Readonly<HorizontalHeroLayoutProps>) {
  return (
    <main className="relative flex min-h-screen flex-col">
      <div className="absolute top-4 right-4 hidden gap-4 rounded-md bg-white p-2 shadow-md lg:flex dark:bg-sidebar">
        <ThemeModeSelector />
        <SupportButton aria-label={t`Contact support`} />
        <LocaleSwitcher />
      </div>
      <div className="flex grow flex-col gap-4 lg:flex-row">
        <div className="flex w-full flex-col items-center justify-center gap-6 bg-background p-6 lg:w-1/2">
          {children}
          <div className="flex gap-4 rounded-md bg-white p-2 shadow-md lg:hidden dark:bg-sidebar">
            <ThemeModeSelector />
            <SupportButton aria-label={t`Contact support`} />
            <LocaleSwitcher />
          </div>
        </div>
        <div className="hidden items-center justify-center bg-input-background p-6 lg:flex lg:w-1/2 lg:px-28 lg:py-12">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
