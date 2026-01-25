import type { ReactNode } from "react";
import LocaleSwitcher from "../common/LocaleSwitcher";
import SupportButton from "../common/SupportButton";
import ThemeModeSelector from "../common/ThemeModeSelector";
import AvatarButton from "./AvatarButton";
import "@repo/ui/tailwind.css";

interface FederatedTopMenuProps {
  children?: ReactNode;
}

export default function FederatedTopMenu({ children }: Readonly<FederatedTopMenuProps>) {
  return (
    <nav className="flex w-full items-center justify-between gap-4">
      <div className="min-w-0 flex-1">{children}</div>
      <div className="flex shrink-0 flex-row items-center gap-6">
        <span className="flex gap-2">
          <ThemeModeSelector />
          <SupportButton />
          <LocaleSwitcher />
        </span>
        <AvatarButton />
      </div>
    </nav>
  );
}

// Re-export AvatarButton for backward compatibility
export { default as AvatarButton } from "./AvatarButton";
