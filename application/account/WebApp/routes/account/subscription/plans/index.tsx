import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { requirePermission } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { useFormatLongDate } from "@repo/ui/hooks/useSmartDate";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { AlertTriangleIcon } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { api, SubscriptionPlan } from "@/shared/lib/api/client";
import { CancelDowngradeDialog } from "../-components/CancelDowngradeDialog";
import { DowngradeConfirmationDialog } from "../-components/DowngradeConfirmationDialog";
import { PlanCard } from "../-components/PlanCard";
import { ReactivateConfirmationDialog } from "../-components/ReactivateConfirmationDialog";
import { SubscriptionTabNavigation } from "../-components/SubscriptionTabNavigation";

export const Route = createFileRoute("/account/subscription/plans/")({
  staticData: { trackingTitle: "Subscription plans" },
  beforeLoad: () => requirePermission({ allowedRoles: ["Owner"] }),
  component: PlansPage
});

function PlansPage() {
  const queryClient = useQueryClient();
  const [isDowngradeDialogOpen, setIsDowngradeDialogOpen] = useState(false);
  const [isCancelDowngradeDialogOpen, setIsCancelDowngradeDialogOpen] = useState(false);
  const [isReactivateDialogOpen, setIsReactivateDialogOpen] = useState(false);
  const [downgradePlan, setDowngradePlan] = useState<SubscriptionPlan>(SubscriptionPlan.Standard);
  const [reactivatePlan, setReactivatePlan] = useState<SubscriptionPlan>(SubscriptionPlan.Standard);

  const { data: subscription } = api.useQuery("get", "/api/account/subscriptions/current");
  const { data: stripeHealth } = api.useQuery("get", "/api/account/subscriptions/stripe-health");

  const checkoutMutation = api.useMutation("post", "/api/account/subscriptions/checkout", {
    onSuccess: (data) => {
      if (data.checkoutUrl) {
        window.location.href = data.checkoutUrl;
      }
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

  const cancelDowngradeMutation = api.useMutation("post", "/api/account/subscriptions/cancel-downgrade", {
    onSuccess: () => {
      setIsCancelDowngradeDialogOpen(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your scheduled downgrade has been cancelled.`);
    }
  });

  const reactivateMutation = api.useMutation("post", "/api/account/subscriptions/reactivate", {
    onSuccess: (data) => {
      if (data.checkoutUrl) {
        window.location.href = data.checkoutUrl;
      } else {
        setIsReactivateDialogOpen(false);
        queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
        toast.success(t`Your subscription has been reactivated.`);
      }
    }
  });

  const formatLongDate = useFormatLongDate();
  const isStripeConfigured = stripeHealth?.isConfigured ?? false;
  const currentPlan = subscription?.plan ?? SubscriptionPlan.Basis;
  const cancelAtPeriodEnd = subscription?.cancelAtPeriodEnd ?? false;
  const scheduledPlan = subscription?.scheduledPlan ?? null;
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

  const isPending =
    checkoutMutation.isPending ||
    upgradeMutation.isPending ||
    downgradeMutation.isPending ||
    cancelDowngradeMutation.isPending ||
    reactivateMutation.isPending;

  const pendingPlan = checkoutMutation.isPending
    ? (checkoutMutation.variables?.body?.plan ?? null)
    : upgradeMutation.isPending
      ? (upgradeMutation.variables?.body?.newPlan ?? null)
      : downgradeMutation.isPending
        ? downgradePlan
        : reactivateMutation.isPending
          ? (reactivateMutation.variables?.body?.plan ?? null)
          : null;

  const handleSubscribe = (plan: SubscriptionPlan) => {
    checkoutMutation.mutate({
      body: {
        plan,
        successUrl: `${window.location.origin}/account/subscription/?session_id={CHECKOUT_SESSION_ID}`,
        cancelUrl: `${window.location.origin}/account/subscription/plans/`
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

  const handleCancelDowngrade = () => {
    setIsCancelDowngradeDialogOpen(true);
  };

  const handleConfirmCancelDowngrade = () => {
    cancelDowngradeMutation.mutate({});
  };

  const handleReactivate = (plan: SubscriptionPlan) => {
    setReactivatePlan(plan);
    setIsReactivateDialogOpen(true);
  };

  const handleConfirmReactivate = () => {
    reactivateMutation.mutate({
      body: {
        plan: reactivatePlan,
        successUrl: `${window.location.origin}/account/subscription/?session_id={CHECKOUT_SESSION_ID}`,
        cancelUrl: `${window.location.origin}/account/subscription/plans/`
      }
    });
  };

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="60rem"
        title={t`Subscription plans`}
        subtitle={t`Manage your subscription and billing.`}
      >
        <SubscriptionTabNavigation activeTab="plans" />

        {cancelAtPeriodEnd && (
          <div className="mb-6 flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
            <AlertTriangleIcon className="size-4 shrink-0" />
            {formattedPeriodEnd ? (
              <Trans>
                Your {getPlanLabel(currentPlan)} subscription has been cancelled and will end on {formattedPeriodEnd}.
                Reactivate by selecting a plan below.
              </Trans>
            ) : (
              <Trans>
                Your subscription has been cancelled and will end at the end of the current billing period. Reactivate
                by selecting a plan below.
              </Trans>
            )}
          </div>
        )}

        {scheduledPlan && !cancelAtPeriodEnd && (
          <div className="mb-6 flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
            <AlertTriangleIcon className="size-4 shrink-0" />
            {formattedPeriodEnd ? (
              <Trans>
                Your subscription will be downgraded to {getPlanLabel(scheduledPlan)} on {formattedPeriodEnd}.
              </Trans>
            ) : (
              <Trans>
                Your subscription will be downgraded to {getPlanLabel(scheduledPlan)} at the end of the current billing
                period.
              </Trans>
            )}
          </div>
        )}

        {!isStripeConfigured && (
          <div className="mb-6 flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
            <AlertTriangleIcon className="size-4 shrink-0" />
            <Trans>Billing is not configured. Please contact support to enable payment processing.</Trans>
          </div>
        )}

        <div className="grid gap-4 sm:grid-cols-3">
          {[SubscriptionPlan.Basis, SubscriptionPlan.Standard, SubscriptionPlan.Premium].map((plan) => (
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
              onCancelDowngrade={handleCancelDowngrade}
              isPending={isPending}
              pendingPlan={pendingPlan}
              isCancelDowngradePending={cancelDowngradeMutation.isPending}
            />
          ))}
        </div>
      </AppLayout>

      <DowngradeConfirmationDialog
        isOpen={isDowngradeDialogOpen}
        onOpenChange={setIsDowngradeDialogOpen}
        onConfirm={handleConfirmDowngrade}
        isPending={downgradeMutation.isPending}
        targetPlan={downgradePlan}
        currentPeriodEnd={currentPeriodEnd}
      />

      {scheduledPlan && (
        <CancelDowngradeDialog
          isOpen={isCancelDowngradeDialogOpen}
          onOpenChange={setIsCancelDowngradeDialogOpen}
          onConfirm={handleConfirmCancelDowngrade}
          isPending={cancelDowngradeMutation.isPending}
          currentPlan={currentPlan}
          scheduledPlan={scheduledPlan}
          currentPeriodEnd={currentPeriodEnd}
        />
      )}

      <ReactivateConfirmationDialog
        isOpen={isReactivateDialogOpen}
        onOpenChange={setIsReactivateDialogOpen}
        onConfirm={handleConfirmReactivate}
        isPending={reactivateMutation.isPending}
        currentPlan={currentPlan}
        targetPlan={reactivatePlan}
      />
    </>
  );
}
