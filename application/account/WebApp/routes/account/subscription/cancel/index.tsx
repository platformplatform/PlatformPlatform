import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { requirePermission } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { useFormatLongDate } from "@repo/ui/hooks/useSmartDate";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { AlertTriangleIcon } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { api, SubscriptionPlan } from "@/shared/lib/api/client";
import { CancelSubscriptionDialog } from "../-components/CancelSubscriptionDialog";
import { CheckoutDialog } from "../-components/CheckoutDialog";
import { ReactivateConfirmationDialog } from "../-components/ReactivateConfirmationDialog";
import { SubscriptionTabNavigation } from "../-components/SubscriptionTabNavigation";

export const Route = createFileRoute("/account/subscription/cancel/")({
  beforeLoad: () => requirePermission({ allowedRoles: ["Owner"] }),
  component: CancelSubscriptionPage
});

function CancelSubscriptionPage() {
  const formatLongDate = useFormatLongDate();
  const queryClient = useQueryClient();
  const [isCancelDialogOpen, setIsCancelDialogOpen] = useState(false);
  const [isReactivateDialogOpen, setIsReactivateDialogOpen] = useState(false);
  const [isCheckoutDialogOpen, setIsCheckoutDialogOpen] = useState(false);
  const [reactivateClientSecret, setReactivateClientSecret] = useState<string | undefined>();
  const [reactivatePublishableKey, setReactivatePublishableKey] = useState<string | undefined>();

  const { data: subscription } = api.useQuery("get", "/api/account/subscriptions/current");

  const cancelMutation = api.useMutation("post", "/api/account/subscriptions/cancel", {
    onSuccess: () => {
      setIsCancelDialogOpen(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your subscription has been cancelled.`);
    }
  });

  const reactivateMutation = api.useMutation("post", "/api/account/subscriptions/reactivate", {
    onSuccess: (data) => {
      if (data.clientSecret && data.publishableKey) {
        setIsReactivateDialogOpen(false);
        setReactivateClientSecret(data.clientSecret);
        setReactivatePublishableKey(data.publishableKey);
        setIsCheckoutDialogOpen(true);
      } else {
        setIsReactivateDialogOpen(false);
        queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
        toast.success(t`Your subscription has been reactivated.`);
      }
    }
  });

  const currentPlan = subscription?.plan ?? SubscriptionPlan.Basis;
  const cancelAtPeriodEnd = subscription?.cancelAtPeriodEnd ?? false;
  const currentPeriodEnd = subscription?.currentPeriodEnd ?? null;
  const formattedPeriodEnd = formatLongDate(currentPeriodEnd);

  function getPlanLabel(plan: SubscriptionPlan): string {
    switch (plan) {
      case SubscriptionPlan.Basis:
        return t`Basis`;
      case SubscriptionPlan.Standard:
        return t`Standard`;
      case SubscriptionPlan.Premium:
        return t`Premium`;
    }
  }

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="60rem"
        title={t`Subscription`}
        subtitle={t`Manage your subscription and billing.`}
      >
        <SubscriptionTabNavigation activeTab="cancel" />

        {cancelAtPeriodEnd ? (
          <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
              <div className="flex items-center gap-3">
                <AlertTriangleIcon className="size-4 shrink-0" />
                {formattedPeriodEnd ? (
                  <Trans>
                    Your {getPlanLabel(currentPlan)} subscription has been cancelled and will end on{" "}
                    {formattedPeriodEnd}.
                  </Trans>
                ) : (
                  <Trans>
                    Your subscription has been cancelled and will end at the end of the current billing period.
                  </Trans>
                )}
              </div>
              <Button size="sm" className="shrink-0" onClick={() => setIsReactivateDialogOpen(true)}>
                <Trans>Reactivate</Trans>
              </Button>
            </div>
          </div>
        ) : (
          <div className="flex flex-col gap-4">
            <p className="text-muted-foreground text-sm">
              {formattedPeriodEnd ? (
                <Trans>
                  If you cancel, you will keep access to your {getPlanLabel(currentPlan)} plan until{" "}
                  {formattedPeriodEnd}. After that, your account will be downgraded.
                </Trans>
              ) : (
                <Trans>
                  If you cancel, you will keep access to your current plan until the end of your billing period. After
                  that, your account will be downgraded.
                </Trans>
              )}
            </p>
            <Button
              variant="destructive"
              className="w-fit"
              onClick={() => setIsCancelDialogOpen(true)}
              disabled={cancelMutation.isPending}
            >
              <Trans>Cancel subscription</Trans>
            </Button>
          </div>
        )}
      </AppLayout>

      <CancelSubscriptionDialog
        isOpen={isCancelDialogOpen}
        onOpenChange={setIsCancelDialogOpen}
        onConfirm={(reason, feedback) => cancelMutation.mutate({ body: { reason, feedback } })}
        isPending={cancelMutation.isPending}
        currentPeriodEnd={currentPeriodEnd}
      />

      <ReactivateConfirmationDialog
        isOpen={isReactivateDialogOpen}
        onOpenChange={setIsReactivateDialogOpen}
        onConfirm={() =>
          reactivateMutation.mutate({
            body: {
              plan: currentPlan,
              returnUrl: `${window.location.origin}/account/subscription/?session_id={CHECKOUT_SESSION_ID}`
            }
          })
        }
        isPending={reactivateMutation.isPending}
        currentPlan={currentPlan}
        targetPlan={currentPlan}
      />

      <CheckoutDialog
        isOpen={isCheckoutDialogOpen}
        onOpenChange={(open) => {
          setIsCheckoutDialogOpen(open);
          if (!open) {
            setReactivateClientSecret(undefined);
            setReactivatePublishableKey(undefined);
          }
        }}
        plan={currentPlan}
        prefetchedClientSecret={reactivateClientSecret}
        prefetchedPublishableKey={reactivatePublishableKey}
      />
    </>
  );
}
