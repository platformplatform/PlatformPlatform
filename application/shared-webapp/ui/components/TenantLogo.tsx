import type { ComponentProps } from "react";

import { cn } from "../utils";
import { Avatar, AvatarFallback, AvatarImage } from "./Avatar";

type TenantLogoProps = {
  /**
   * The URL of the logo to display.
   */
  logoUrl?: string | null;
  /**
   * The tenant name to use for initials.
   */
  tenantName: string;
  /**
   * The size of the logo.
   */
  size?: "sm" | "md" | "lg";
  /**
   * Additional class names to apply to the logo.
   */
  className?: string;
} & Omit<ComponentProps<typeof Avatar>, "className" | "size">;

const sizeMap = {
  sm: "size-6",
  md: "size-8",
  lg: "size-16"
};

const fallbackTextSize = {
  sm: "text-[0.625rem]",
  md: "text-xs",
  lg: "text-xl"
};

function getTenantInitials(tenantName: string): string {
  const cleaned = tenantName.trim();
  if (cleaned.length === 0) {
    return "?";
  }
  const parts = cleaned.split(/\s+/);
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}

export function TenantLogo({ logoUrl, tenantName, size = "md", className, ...props }: Readonly<TenantLogoProps>) {
  return (
    <Avatar className={cn(sizeMap[size], "shrink-0 rounded-lg after:hidden", className)} {...props}>
      <AvatarImage src={logoUrl ?? undefined} className="rounded-lg object-contain" />
      <AvatarFallback className={cn("rounded-lg bg-muted font-medium text-muted-foreground", fallbackTextSize[size])}>
        {getTenantInitials(tenantName)}
      </AvatarFallback>
    </Avatar>
  );
}
