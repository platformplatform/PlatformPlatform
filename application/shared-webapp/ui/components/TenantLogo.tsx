import { cva, type VariantProps } from "class-variance-authority";
import { ImageIcon } from "lucide-react";
import { type HTMLAttributes, useCallback, useRef, useState } from "react";
import logoMarkUrl from "../images/logo-mark.svg";
import { cn } from "../utils";

const backgroundStyles = cva(
  "relative inline-flex shrink-0 items-center justify-center overflow-hidden font-semibold uppercase",
  {
    variants: {
      isRound: {
        true: "rounded-full",
        false: "rounded-md"
      },
      size: {
        xs: "h-8 w-8 text-xs",
        lg: "h-16 w-16 text-lg"
      }
    },
    defaultVariants: {
      isRound: false,
      size: "xs"
    }
  }
);

export type TenantLogoProps = {
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
  size?: VariantProps<typeof backgroundStyles>["size"];
  /**
   * Whether the logo should be round or rounded.
   */
  isRound?: boolean;
  /**
   * Additional class names to apply to the logo.
   */
  className?: string;
} & HTMLAttributes<HTMLImageElement>;

export function TenantLogo({ logoUrl, tenantName, size, isRound, className, ...props }: TenantLogoProps) {
  const imgRef = useRef<HTMLImageElement>(null);
  const [imageFailed, setImageFailed] = useState(false);
  const [imageLoaded, setImageLoaded] = useState(false);

  const handleError = useCallback(() => setImageFailed(true), []);
  const handleLoad = useCallback(() => setImageLoaded(true), []);

  const initial = tenantName?.charAt(0).toUpperCase() || "";
  const hasName = tenantName && tenantName.trim().length > 0;

  // Different defaults based on size
  // xs (sidebar) - use PlatformPlatform logo mark
  // lg (account settings) - use placeholder icon
  const renderFallback = () => {
    if (hasName) {
      return initial;
    }

    if (size === "lg") {
      // Account settings - show placeholder icon
      return <ImageIcon className="h-10 w-10 text-muted-foreground" />;
    }
    // Sidebar - show PlatformPlatform logo mark
    return <img src={logoMarkUrl} alt="PlatformPlatform" className="h-full w-full object-contain" />;
  };

  return (
    <div {...props} className={cn(backgroundStyles({ isRound, size }), className)}>
      {logoUrl && !imageFailed ? (
        <>
          <img
            key={logoUrl}
            ref={imgRef}
            className={`h-full w-full object-contain ${imageLoaded ? "" : "invisible"}`}
            src={logoUrl}
            alt="Tenant logo"
            onError={handleError}
            onLoad={handleLoad}
            style={{ position: "absolute", inset: 0 }}
          />
          {!imageLoaded && renderFallback()}
        </>
      ) : (
        renderFallback()
      )}
    </div>
  );
}
