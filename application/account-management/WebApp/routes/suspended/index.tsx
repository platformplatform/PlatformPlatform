import { TenantState, api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { LocaleSwitcher } from "@repo/infrastructure/translations/LocaleSwitcher";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { useToast } from "@repo/ui/hooks/useToast";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { createFileRoute } from "@tanstack/react-router";
import { LifeBuoyIcon, LogOutIcon, PlayCircleIcon } from "lucide-react";
import { useState } from "react";

export const Route = createFileRoute("/suspended/")({
  component: SuspendedTenantPage
});

export function SuspendedTenantPage() {
  const [isReactivating, setIsReactivating] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const { toast } = useToast();
  const userInfo = useUserInfo();
  const isOwner = userInfo?.role === "Owner";
  const changeTenantStateMutation = api.useMutation("put", "/api/account-management/tenants/current/state");
  const logoutMutation = api.useMutation("post", "/api/account-management/authentication/logout");

  const handleReactivate = async () => {
    setIsReactivating(true);
    await changeTenantStateMutation.mutateAsync({ body: { newState: TenantState.Active } });
    toast({
      title: t`Account activated`,
      description: t`Your account has been reactivated successfully.`,
      variant: "success"
    });
    // Refresh the page to apply the new tenant state
    window.location.reload();
    setIsReactivating(false);
  };

  const handleLogout = async () => {
    setIsLoggingOut(true);
    await logoutMutation.mutateAsync({});
    // Redirect to login page
    window.location.href = "/login";
  };

  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center bg-background p-4">
      <div className="absolute top-4 right-4 hidden gap-4 rounded-md bg-white p-2 shadow-md sm:flex dark:bg-gray-800">
        <ThemeModeSelector aria-label={t`Toggle theme`} />
        <Button variant="icon" aria-label={t`Help`}>
          <LifeBuoyIcon size={20} />
        </Button>
        <LocaleSwitcher aria-label={t`Select language`} />
      </div>
      <div className="w-full max-w-md rounded-lg border border-warning bg-warning/10 p-8 shadow-lg">
        <div className="mb-6 flex items-center justify-between">
          <h1 className="font-bold text-2xl text-foreground">
            <Trans>Account suspended</Trans>
          </h1>
          <Badge variant="warning">
            <Trans>Suspended</Trans>
          </Badge>
        </div>

        <div className="mb-8 space-y-4 text-muted-foreground">
          <p>
            <Trans>This account is currently suspended, and all features are disabled.</Trans>
          </p>

          {isOwner ? (
            <p>
              <Trans>As an account owner, you can reactivate this account.</Trans>
            </p>
          ) : (
            <p>
              <Trans>Please contact your account owner.</Trans>
            </p>
          )}
        </div>

        {isOwner ? (
          <Button className="mb-4 w-full" onPress={handleReactivate} isDisabled={isReactivating}>
            <PlayCircleIcon className="mr-2 h-4 w-4" />
            {isReactivating ? <Trans>Reactivating...</Trans> : <Trans>Reactivate account</Trans>}
          </Button>
        ) : null}

        <Button className="w-full" variant="outline" onPress={handleLogout} isDisabled={isLoggingOut}>
          <LogOutIcon className="mr-2 h-4 w-4" />
          {isLoggingOut ? <Trans>Logging out...</Trans> : <Trans>Log out</Trans>}
        </Button>
      </div>
    </div>
  );
}
