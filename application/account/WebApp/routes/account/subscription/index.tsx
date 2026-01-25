import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { hasPermission } from "@repo/infrastructure/auth/routeGuards";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { AlertTriangleIcon, ExternalLinkIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { AppLayout } from "@/shared/components/AppLayout";
import { api, SubscriptionPlan } from "@/shared/lib/api/client";
import { BillingHistoryTable } from "./-components/BillingHistoryTable";
import { CancelSubscriptionDialog } from "./-components/CancelSubscriptionDialog";
import { DowngradeConfirmationDialog } from "./-components/DowngradeConfirmationDialog";
import { PlanCard } from "./-components/PlanCard";
import { ProcessingPaymentModal } from "./-components/ProcessingPaymentModal";

export const Route = createFileRoute("/account/subscription/")({
  component: SubscriptionPage
});

function AccessDenied() {
  return (
    <AppLayout variant="center" title={t`Access denied`} subtitle={t`Only account owners can manage the subscription.`}>
      <div />
    </AppLayout>
  );
}

function SubscriptionPage() {
  const isOwner = hasPermission({ allowedRoles: ["Owner"] });
  const queryClient = useQueryClient();
  const [isCancelDialogOpen, setIsCancelDialogOpen] = useState(false);
  const [isDowngradeDialogOpen, setIsDowngradeDialogOpen] = useState(false);
  const [downgradePlan, setDowngradePlan] = useState<SubscriptionPlan>(SubscriptionPlan.Standard);
  const [isProcessing, setIsProcessing] = useState(false);

  const { data: subscription } = api.useQuery("get", "/api/account/subscriptions/current", {
    enabled: isOwner
  });
  const { data: stripeHealth } = api.useQuery("get", "/api/account/subscriptions/stripe-health", {
    enabled: isOwner
  });

  const checkoutMutation = api.useMutation("post", "/api/account/subscriptions/checkout", {
    onSuccess: (data) => {
      if (data.checkoutUrl) {
        window.location.href = data.checkoutUrl;
      }
    }
  });

  const checkoutSuccessMutation = api.useMutation("post", "/api/account/subscriptions/checkout-success", {
    onSuccess: () => {
      setIsProcessing(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your subscription has been activated.`);
    }
  });

  const upgradeMutation = api.useMutation("post", "/api/account/subscriptions/upgrade", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your plan has been upgraded.`);
    }
  });

  const downgradeMutation = api.useMutation("post", "/api/account/subscriptions/schedule-downgrade", {
    onSuccess: () => {
      setIsDowngradeDialogOpen(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your downgrade has been scheduled.`);
    }
  });

  const cancelMutation = api.useMutation("post", "/api/account/subscriptions/cancel", {
    onSuccess: () => {
      setIsCancelDialogOpen(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your subscription has been cancelled.`);
    }
  });

  const reactivateMutation = api.useMutation("post", "/api/account/subscriptions/reactivate", {
    onSuccess: (data) => {
      if (data.checkoutUrl) {
        window.location.href = data.checkoutUrl;
      } else {
        queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
        toast.success(t`Your subscription has been reactivated.`);
      }
    }
  });

  const billingPortalMutation = api.useMutation("post", "/api/account/subscriptions/billing-portal", {
    onSuccess: (data) => {
      if (data.portalUrl) {
        window.location.href = data.portalUrl;
      }
    }
  });

  useEffect(() => {
    if (!isOwner) {
      return;
    }
    const urlParams = new URLSearchParams(window.location.search);
    const sessionId = urlParams.get("session_id");

    if (sessionId) {
      setIsProcessing(true);
      window.history.replaceState({}, "", window.location.pathname);
      checkoutSuccessMutation.mutate({ body: { sessionId } });
    }
  }, []);

  if (!isOwner) {
    return <AccessDenied />;
  }

  const isStripeConfigured = stripeHealth?.isConfigured ?? false;

  const handleSubscribe = (plan: SubscriptionPlan) => {
    checkoutMutation.mutate({
      body: {
        plan,
        successUrl: `${window.location.origin}/account/subscription/?session_id={CHECKOUT_SESSION_ID}`,
        cancelUrl: `${window.location.origin}/account/subscription/`
      }
    });
  };

  const handleUpgrade = (plan: SubscriptionPlan) => {
    upgradeMutation.mutate({ body: { newPlan: plan } });
  };

  const handleDowngrade = (plan: SubscriptionPlan) => {
    setDowngradePlan(plan);
    setIsDowngradeDialogOpen(true);
  };

  const handleConfirmDowngrade = () => {
    downgradeMutation.mutate({ body: { newPlan: downgradePlan } });
  };

  const handleCancel = () => {
    cancelMutation.mutate({ body: {} });
  };

  const handleReactivate = (plan: SubscriptionPlan) => {
    reactivateMutation.mutate({
      body: {
        plan,
        successUrl: `${window.location.origin}/account/subscription/?session_id={CHECKOUT_SESSION_ID}`,
        cancelUrl: `${window.location.origin}/account/subscription/`
      }
    });
  };

  const currentPlan = subscription?.plan ?? SubscriptionPlan.Trial;
  const cancelAtPeriodEnd = subscription?.cancelAtPeriodEnd ?? false;
  const scheduledPlan = subscription?.scheduledPlan ?? null;
  const currentPeriodEnd = subscription?.currentPeriodEnd ?? null;
  const hasStripeSubscription = subscription?.hasStripeSubscription ?? false;

  const isPending =
    checkoutMutation.isPending ||
    upgradeMutation.isPending ||
    downgradeMutation.isPending ||
    cancelMutation.isPending ||
    reactivateMutation.isPending;

  const formattedPeriodEnd = currentPeriodEnd
    ? new Date(currentPeriodEnd).toLocaleDateString(undefined, {
        year: "numeric",
        month: "long",
        day: "numeric"
      })
    : null;

  function getPlanLabel(plan: SubscriptionPlan): string {
    switch (plan) {
      case SubscriptionPlan.Trial:
        return t`Trial`;
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
        maxWidth="960px"
        title={t`Subscription`}
        subtitle={t`Manage your subscription and billing.`}
      >
        {!isStripeConfigured && (
          <div className="mb-6 flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
            <AlertTriangleIcon className="size-4 shrink-0" />
            <Trans>Billing is not configured. Please contact support to enable payment processing.</Trans>
          </div>
        )}

        <div className="flex flex-col gap-4">
          <h3>
            <Trans>Current plan</Trans>
          </h3>
          <Separator />
          <div className="flex flex-wrap items-center gap-3">
            <span className="font-medium">{getPlanLabel(currentPlan)}</span>
            {cancelAtPeriodEnd ? (
              <Badge variant="destructive">
                <Trans>Cancelling</Trans>
              </Badge>
            ) : (
              hasStripeSubscription && (
                <Badge variant="default">
                  <Trans>Active</Trans>
                </Badge>
              )
            )}
            {scheduledPlan && (
              <Badge variant="secondary">
                <Trans>Downgrading to {getPlanLabel(scheduledPlan)}</Trans>
              </Badge>
            )}
          </div>
          {formattedPeriodEnd && hasStripeSubscription && (
            <p className="text-muted-foreground text-sm">
              {cancelAtPeriodEnd ? (
                <Trans>Access until {formattedPeriodEnd}</Trans>
              ) : (
                <Trans>Next billing date: {formattedPeriodEnd}</Trans>
              )}
            </p>
          )}
          {hasStripeSubscription && (
            <Button
              variant="outline"
              className="w-fit"
              onClick={() =>
                billingPortalMutation.mutate({
                  body: { returnUrl: window.location.href }
                })
              }
              disabled={billingPortalMutation.isPending || !isStripeConfigured}
            >
              <ExternalLinkIcon className="size-4" />
              {billingPortalMutation.isPending ? t`Loading...` : t`Manage payment method`}
            </Button>
          )}
        </div>

        <div className="mt-8 flex flex-col gap-4">
          <h3>{cancelAtPeriodEnd ? <Trans>Choose a plan to reactivate</Trans> : <Trans>Plans</Trans>}</h3>
          <Separator />
          <div className="grid gap-4 sm:grid-cols-3">
            {[SubscriptionPlan.Trial, SubscriptionPlan.Standard, SubscriptionPlan.Premium].map((plan) => (
              <PlanCard
                key={plan}
                plan={plan}
                currentPlan={currentPlan}
                cancelAtPeriodEnd={cancelAtPeriodEnd}
                scheduledPlan={scheduledPlan}
                isStripeConfigured={isStripeConfigured}
                onSubscribe={handleSubscribe}
                onUpgrade={handleUpgrade}
                onDowngrade={handleDowngrade}
                onReactivate={handleReactivate}
                isPending={isPending}
              />
            ))}
          </div>
        </div>

        {hasStripeSubscription && !cancelAtPeriodEnd && (
          <div className="mt-8 flex flex-col gap-4">
            <h3>
              <Trans>Cancel subscription</Trans>
            </h3>
            <Separator />
            <p className="text-muted-foreground text-sm">
              <Trans>
                Cancel your subscription. You will keep access to your current plan until the end of your billing
                period.
              </Trans>
            </p>
            <Button
              variant="destructive"
              className="w-fit"
              onClick={() => setIsCancelDialogOpen(true)}
              disabled={isPending}
            >
              <Trans>Cancel subscription</Trans>
            </Button>
          </div>
        )}

        <div className="mt-8 flex flex-col gap-4">
          <h3>
            <Trans>Billing history</Trans>
          </h3>
          <Separator />
          <BillingHistoryTable />
        </div>
      </AppLayout>

      <CancelSubscriptionDialog
        isOpen={isCancelDialogOpen}
        onOpenChange={setIsCancelDialogOpen}
        onConfirm={handleCancel}
        isPending={cancelMutation.isPending}
        currentPeriodEnd={currentPeriodEnd}
      />

      <DowngradeConfirmationDialog
        isOpen={isDowngradeDialogOpen}
        onOpenChange={setIsDowngradeDialogOpen}
        onConfirm={handleConfirmDowngrade}
        isPending={downgradeMutation.isPending}
        targetPlan={downgradePlan}
        currentPeriodEnd={currentPeriodEnd}
      />

      <ProcessingPaymentModal isOpen={isProcessing} />
    </>
  );
}
