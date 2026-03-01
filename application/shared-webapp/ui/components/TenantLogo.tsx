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

export function TenantLogo({ logoUrl, tenantName: _, size = "md", className, ...props }: Readonly<TenantLogoProps>) {
  const iconSize = size === "lg" ? "size-12" : size === "md" ? "size-7" : "size-5";
  const iconPadding = "p-0.5";

  return (
    <Avatar className={cn(sizeMap[size], "shrink-0 rounded-lg after:hidden", className)} {...props}>
      <AvatarImage src={logoUrl ?? undefined} className="rounded-lg object-contain" />
      <AvatarFallback className={cn("rounded-lg bg-transparent", iconPadding)}>
        <Building2Icon className={`${iconSize} text-muted-foreground`} />
      </AvatarFallback>
    </Avatar>
  );
}
