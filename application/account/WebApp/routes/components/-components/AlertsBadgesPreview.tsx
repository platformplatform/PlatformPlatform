import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Alert, AlertDescription, AlertTitle } from "@repo/ui/components/Alert";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { AlertCircleIcon, FlagIcon, InfoIcon, TriangleAlertIcon, XIcon } from "lucide-react";
import { useState } from "react";
import { createPortal } from "react-dom";

import { ItemPreview } from "./ItemPreview";
import { ProgressPreview } from "./ProgressPreview";
import { SpinnerPreview } from "./SpinnerPreview";

type BannerVariant = "persistent" | "dismissable" | "cta";

const bannerContent: Record<BannerVariant, { message: () => string; icon: React.ReactNode }> = {
  persistent: {
    message: () => t`Scheduled maintenance on Sunday 2:00 AM — 4:00 AM UTC. Some features may be unavailable.`,
    icon: <InfoIcon className="size-4 shrink-0 text-warning-foreground" />
  },
  dismissable: {
    message: () => t`75% of quota used`,
    icon: <FlagIcon className="size-4 shrink-0 text-warning-foreground" />
  },
  cta: {
    message: () => t`Your trial expires in 3 days.`,
    icon: <AlertCircleIcon className="size-4 shrink-0 text-warning-foreground" />
  }
};

function SampleBanner({ variant, onClose }: Readonly<{ variant: BannerVariant; onClose: () => void }>) {
  const [target] = useState(() => document.getElementById("banner-root"));
  if (!target) return null;

  const content = bannerContent[variant];

  return createPortal(
    <div className="flex h-12 items-center gap-3 border-b border-warning/50 bg-warning px-4 text-sm">
      {content.icon}
      <span className="flex-1 text-warning-foreground">{content.message()}</span>
      {variant === "cta" && (
        <Button size="sm" onClick={onClose}>
          <Trans>Upgrade now</Trans>
        </Button>
      )}
      {variant === "dismissable" && (
        <Button variant="ghost" size="icon-sm" aria-label={t`Close`} onClick={onClose}>
          <XIcon className="size-4" />
        </Button>
      )}
    </div>,
    target
  );
}

function BannersSection() {
  const [bannerMode, setBannerMode] = useState<"none" | BannerVariant>("none");

  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Banners</Trans>
      </h4>
      <div className="flex flex-wrap items-center gap-3">
        <Button
          variant={bannerMode === "persistent" ? "default" : "outline"}
          onClick={() => setBannerMode(bannerMode === "persistent" ? "none" : "persistent")}
        >
          <Trans>Show banner</Trans>
        </Button>
        <Button
          variant={bannerMode === "dismissable" ? "default" : "outline"}
          onClick={() => setBannerMode(bannerMode === "dismissable" ? "none" : "dismissable")}
        >
          <Trans>Show dismissable banner</Trans>
        </Button>
        <Button
          variant={bannerMode === "cta" ? "default" : "outline"}
          onClick={() => setBannerMode(bannerMode === "cta" ? "none" : "cta")}
        >
          <Trans>Show banner with action</Trans>
        </Button>
      </div>
      {bannerMode !== "none" && <SampleBanner variant={bannerMode} onClose={() => setBannerMode("none")} />}
    </div>
  );
}

export function AlertsBadgesPreview() {
  return (
    <div className="flex flex-col gap-6">
      <BannersSection />

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Alerts</Trans>
        </h4>
        <div className="grid grid-cols-2 gap-4">
          <Alert>
            <InfoIcon />
            <AlertTitle>
              <Trans>Default alert</Trans>
            </AlertTitle>
            <AlertDescription>
              <Trans>This is a default informational alert.</Trans>
            </AlertDescription>
          </Alert>
          <Alert variant="info">
            <InfoIcon />
            <AlertTitle>
              <Trans>Info alert</Trans>
            </AlertTitle>
            <AlertDescription>
              <Trans>This is an informational message.</Trans>
            </AlertDescription>
          </Alert>
          <Alert variant="warning">
            <TriangleAlertIcon />
            <AlertTitle>
              <Trans>Warning alert</Trans>
            </AlertTitle>
            <AlertDescription>
              <Trans>This action may have unintended consequences.</Trans>
            </AlertDescription>
          </Alert>
          <Alert variant="destructive">
            <AlertCircleIcon />
            <AlertTitle>
              <Trans>Destructive alert</Trans>
            </AlertTitle>
            <AlertDescription>
              <Trans>Something went wrong. Please try again.</Trans>
            </AlertDescription>
          </Alert>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Badges</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-3">
          <Badge>
            <Trans>Default</Trans>
          </Badge>
          <Badge variant="secondary">
            <Trans>Secondary</Trans>
          </Badge>
          <Badge variant="destructive">
            <Trans>Destructive</Trans>
          </Badge>
          <Badge variant="warning">
            <Trans>Warning</Trans>
          </Badge>
          <Badge variant="outline">
            <Trans>Outline</Trans>
          </Badge>
        </div>
      </div>

      <div className="flex flex-col gap-3">
        <h4>
          <Trans>Separator</Trans>
        </h4>
        <div className="flex flex-col gap-3 rounded-md border border-border bg-card p-4">
          <span className="text-sm font-medium">
            <Trans>Horizontal</Trans>
          </span>
          <Separator />
          <span className="text-sm text-muted-foreground">
            <Trans>Use a horizontal separator to group related sections in a column layout.</Trans>
          </span>
        </div>
        <div className="flex h-12 items-center gap-4 rounded-md border border-border bg-card px-4">
          <span className="text-sm">
            <Trans>Profile</Trans>
          </span>
          <Separator orientation="vertical" />
          <span className="text-sm">
            <Trans>Settings</Trans>
          </span>
          <Separator orientation="vertical" />
          <span className="text-sm">
            <Trans>Billing</Trans>
          </span>
        </div>
      </div>

      <ProgressPreview />
      <SpinnerPreview />
      <ItemPreview />
    </div>
  );
}
