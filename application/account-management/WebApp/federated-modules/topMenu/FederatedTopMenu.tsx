import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import type { ReactNode } from "react";
import { Suspense, lazy } from "react";
import AvatarButton from "./AvatarButton";
import LocaleSwitcher from "./LocaleSwitcher";
import ThemeModeSelector from "./ThemeModeSelector";
import "@repo/ui/tailwind.css";

const SupportButton = lazy(() => import("../support/SupportButton"));

interface FederatedTopMenuProps {
  children?: ReactNode;
}

export default function FederatedTopMenu({ children }: Readonly<FederatedTopMenuProps>) {
  return (
    <nav className="flex w-full items-center justify-between">
      {children}
      <div className="flex flex-row items-center gap-6">
        <span className="flex gap-2">
          <ThemeModeSelector />
          <Suspense fallback={<Button variant="icon" isDisabled={true} />}>
            <SupportButton aria-label={t`Contact support`} />
          </Suspense>
          <LocaleSwitcher />
        </span>
        <AvatarButton aria-label={t`User profile menu`} />
      </div>
    </nav>
  );
}

// Re-export AvatarButton for backward compatibility
export { default as AvatarButton } from "./AvatarButton";
