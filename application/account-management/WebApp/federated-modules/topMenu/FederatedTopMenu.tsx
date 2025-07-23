import { t } from "@lingui/core/macro";
import type { ReactNode } from "react";
import AvatarButton from "./AvatarButton";
import "@repo/ui/tailwind.css";

interface FederatedTopMenuProps {
  children?: ReactNode;
  rightContent?: ReactNode;
}

export default function FederatedTopMenu({ children, rightContent }: Readonly<FederatedTopMenuProps>) {
  return (
    <nav className="flex w-full items-center justify-between">
      {children}
      <div className="flex flex-row items-center gap-6">
        {rightContent}
        <AvatarButton aria-label={t`User profile menu`} />
      </div>
    </nav>
  );
}

// Re-export AvatarButton for backward compatibility
export { default as AvatarButton } from "./AvatarButton";
