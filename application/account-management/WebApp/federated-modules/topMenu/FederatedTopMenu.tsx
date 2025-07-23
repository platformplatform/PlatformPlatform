import type { ReactNode } from "react";
import "@repo/ui/tailwind.css";

interface FederatedTopMenuProps {
  children?: ReactNode;
}

export default function FederatedTopMenu({ children }: Readonly<FederatedTopMenuProps>) {
  return <nav className="flex w-full items-center justify-between">{children}</nav>;
}
