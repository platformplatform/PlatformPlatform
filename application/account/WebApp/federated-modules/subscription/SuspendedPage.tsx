import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon } from "lucide-react";

export default function SuspendedPage() {
  const userInfo = useUserInfo();
  const isOwner = userInfo?.role === "Owner";

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-6 px-4">
      <div className="flex flex-col items-center gap-4 text-center">
        <div className="inline-flex size-16 items-center justify-center rounded-full bg-destructive/10">
          <AlertTriangleIcon className="size-8 text-destructive" />
        </div>
        <h1>
          <Trans>Payment failed</Trans>
        </h1>
        <p className="max-w-md text-muted-foreground">
          {isOwner ? (
            <Trans>
              Your subscription has been suspended due to a failed payment. Please update your payment method to restore
              access.
            </Trans>
          ) : (
            <Trans>
              Your subscription has been suspended due to a failed payment. Please contact the account owner to restore
              access.
            </Trans>
          )}
        </p>
      </div>
      {isOwner && (
        <div className="flex gap-3">
          <Button onClick={() => (window.location.href = "/account/subscription")}>
            <Trans>Update payment method</Trans>
          </Button>
          <Button variant="outline" onClick={() => (window.location.href = "/account/subscription")}>
            <Trans>Reactivate subscription</Trans>
          </Button>
        </div>
      )}
    </div>
  );
}
