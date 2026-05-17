import type { ReactNode } from "react";

import { t } from "@lingui/core/macro";
import heroDesktopBlurImage from "@repo/ui/images/hero-desktop-blur.webp";
import heroDesktopImage from "@repo/ui/images/hero-desktop-xl.webp";

import { LoginFooterControls } from "@/routes/-components/LoginFooterControls";

interface HorizontalHeroLayoutProps {
  children: ReactNode;
}

export function HorizontalHeroLayout({ children }: Readonly<HorizontalHeroLayoutProps>) {
  return (
    <main className="relative flex min-h-screen flex-col">
      <div className="absolute top-4 right-4 hidden lg:block">
        <LoginFooterControls />
      </div>
      <div className="flex grow flex-col gap-4 lg:flex-row">
        <div className="flex min-h-screen w-full flex-col bg-background p-6 lg:min-h-0 lg:w-1/2">
          <div style={{ flex: 1 }} className="flex flex-col items-center justify-center gap-6">
            {children}
            <div className="lg:hidden">
              <LoginFooterControls />
            </div>
          </div>
        </div>
        <div className="hidden items-center justify-center bg-input-background p-6 lg:flex lg:w-1/2 lg:px-28 lg:py-12">
          <div
            className="h-auto w-full max-w-[64rem] bg-cover bg-center bg-no-repeat"
            style={{ backgroundImage: `url(${heroDesktopBlurImage})`, aspectRatio: "1000/760" }}
          >
            <img
              src={heroDesktopImage}
              alt={t`Screenshots of the dashboard project with desktop and mobile versions`}
              fetchPriority="high"
              className="h-auto w-full"
            />
          </div>
        </div>
      </div>
    </main>
  );
}
