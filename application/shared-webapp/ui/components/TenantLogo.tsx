import { Building2Icon } from "lucide-react";
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

export function TenantLogo({ logoUrl, tenantName, size = "md", className, ...props }: Readonly<TenantLogoProps>) {
  const initial = tenantName?.charAt(0).toUpperCase() || "";
  const hasName = tenantName && tenantName.trim().length > 0;

  // Fallback strategy (following Slack/Linear pattern):
  // - If has name: show initial letter
  // - If no name: show generic building icon
  const iconSize = size === "lg" ? "size-12" : size === "md" ? "size-7" : "size-5";
  const iconPadding = "p-0.5";
  const imagePadding = size === "lg" ? "p-1" : "";

  return (
    <Avatar className={cn(sizeMap[size], "shrink-0 rounded-lg after:hidden", className)} {...props}>
      <AvatarImage src={logoUrl ?? undefined} className={cn("rounded-lg object-contain", imagePadding)} />
      <AvatarFallback className={cn("rounded-lg bg-transparent", hasName ? "p-0.5" : iconPadding)}>
        {hasName ? initial : <Building2Icon className={`${iconSize} text-muted-foreground`} />}
      </AvatarFallback>
    </Avatar>
  );
}
