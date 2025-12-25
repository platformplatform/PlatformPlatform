import { CircleCheckIcon, InfoIcon, Loader2Icon, OctagonXIcon, TriangleAlertIcon } from "lucide-react";
import { Toaster as SonnerToaster, type ToasterProps } from "sonner";
import { useThemeMode } from "../theme/mode/ThemeMode";

function Toaster({ ...props }: ToasterProps) {
  const { resolvedThemeMode } = useThemeMode();

  return (
    <SonnerToaster
      theme={resolvedThemeMode}
      className="toaster group"
      position="top-right"
      containerAriaLabel="Notifications"
      closeButton={true}
      toastOptions={{
        classNames: {
          toast:
            "group toast group-[.toaster]:bg-background group-[.toaster]:text-foreground group-[.toaster]:border-border group-[.toaster]:shadow-lg",
          description: "group-[.toast]:text-muted-foreground",
          actionButton: "group-[.toast]:bg-primary group-[.toast]:text-primary-foreground",
          cancelButton: "group-[.toast]:bg-muted group-[.toast]:text-muted-foreground",
          success:
            "group-[.toaster]:bg-success group-[.toaster]:text-success-foreground group-[.toaster]:border-success",
          warning:
            "group-[.toaster]:bg-warning group-[.toaster]:text-warning-foreground group-[.toaster]:border-warning",
          error: "group-[.toaster]:bg-danger group-[.toaster]:text-danger-foreground group-[.toaster]:border-danger",
          info: "group-[.toaster]:bg-info group-[.toaster]:text-info-foreground group-[.toaster]:border-info"
        }
      }}
      icons={{
        success: <CircleCheckIcon className="size-4" />,
        info: <InfoIcon className="size-4" />,
        warning: <TriangleAlertIcon className="size-4" />,
        error: <OctagonXIcon className="size-4" />,
        loading: <Loader2Icon className="size-4 animate-spin" />
      }}
      style={
        {
          "--normal-bg": "var(--popover)",
          "--normal-text": "var(--popover-foreground)",
          "--normal-border": "var(--border)",
          "--border-radius": "var(--radius)",
          zIndex: 60
        } as React.CSSProperties
      }
      {...props}
    />
  );
}

export { Toaster };
