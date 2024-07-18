/**
 * ref: https://react.fluentui.dev/?path=/docs/components-avatar--default
 * ref: https://ui.shadcn.com/docs/components/avatar
 */
import { type HTMLAttributes, useCallback, useRef, useState } from "react";
import { tv } from "tailwind-variants";

type AvatarProps = {
  /**
   * The two-letter initials to display.
   */
  initials: string;
  /**
   * The URL of the image to display.
   */
  avatarUrl?: string | null;
  /**
   * The size of the avatar.
   */
  size?: "xs" | "sm" | "md" | "lg";
  /**
   * Whether the avatar should be round or rounded.
   */
  isRound?: boolean;
  /**
   * The variant of the avatar.
   */
  variant?: "background" | "primary" | "secondary" | "success" | "warning" | "danger" | "info";
  /**
   * Additional class names to apply to the avatar.
   */
  className?: string;
} & HTMLAttributes<HTMLImageElement>;

const backgroundStyles = tv({
  base: "inline-flex shrink-0 items-center border border-border justify-center overflow-hidden font-semibold uppercase",
  variants: {
    isRound: {
      true: "rounded-full",
      false: "rounded-md"
    },
    size: {
      xs: "h-8 w-8 text-xs",
      sm: "h-10 w-10 text-sm",
      md: "h-12 w-12 text-base",
      lg: "h-16 w-16 text-lg"
    },
    variant: {
      background: "bg-background text-foreground",
      primary: "bg-primary text-primary-foreground",
      secondary: "bg-secondary text-secondary-foreground",
      success: "bg-success text-success-foreground",
      warning: "bg-warning text-warning-foreground",
      danger: "bg-danger text-danger-foreground",
      info: "bg-info text-info-foreground"
    }
  },
  defaultVariants: {
    isRound: false,
    size: "md",
    variant: "background"
  }
});

export function Avatar({ initials, avatarUrl, size, variant, isRound, className, ...props }: AvatarProps) {
  const imgRef = useRef<HTMLImageElement>(null);
  const [imageFailed, setImageFailed] = useState(false);

  const handleError = useCallback(() => setImageFailed(true), []);

  return (
    <div {...props} className={backgroundStyles({ isRound, size, variant, className })}>
      {avatarUrl && imageFailed === false ? (
        <img ref={imgRef} className="" src={avatarUrl} alt="User avatar" onError={handleError} />
      ) : (
        initials?.slice(0, 2)
      )}
    </div>
  );
}
