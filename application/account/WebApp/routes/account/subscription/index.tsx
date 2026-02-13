import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { requirePermission } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { useFormatLongDate } from "@repo/ui/hooks/useSmartDate";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { AlertTriangleIcon, PencilIcon, RefreshCwIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, SubscriptionPlan } from "@/shared/lib/api/client";
import { BillingHistoryTable } from "./-components/BillingHistoryTable";
import { BillingInfoDisplay } from "./-components/BillingInfoDisplay";
import { CancelDowngradeDialog } from "./-components/CancelDowngradeDialog";
import { EditBillingInfoDialog } from "./-components/EditBillingInfoDialog";
import { PaymentMethodDisplay } from "./-components/PaymentMethodDisplay";
import { PlanCard } from "./-components/PlanCard";
import { ProcessingPaymentModal } from "./-components/ProcessingPaymentModal";
import { ReactivateConfirmationDialog } from "./-components/ReactivateConfirmationDialog";
import { SubscriptionTabNavigation } from "./-components/SubscriptionTabNavigation";
import { UpdatePaymentMethodDialog } from "./-components/UpdatePaymentMethodDialog";

export const Route = createFileRoute("/account/subscription/")({
  staticData: { trackingTitle: "Subscription" },
  beforeLoad: () => requirePermission({ allowedRoles: ["Owner"] }),
  component: SubscriptionPage
});

function SubscriptionPage() {
  const formatLongDate = useFormatLongDate();
  const queryClient = useQueryClient();
  const [isProcessing, setIsProcessing] = useState(false);
  const [isCancelDowngradeDialogOpen, setIsCancelDowngradeDialogOpen] = useState(false);
  const [isReactivateDialogOpen, setIsReactivateDialogOpen] = useState(false);
  const [isEditBillingInfoOpen, setIsEditBillingInfoOpen] = useState(false);
  const [isUpdatePaymentMethodOpen, setIsUpdatePaymentMethodOpen] = useState(false);

  const { data: subscription } = api.useQuery("get", "/api/account/subscriptions/current");
  const { data: stripeHealth } = api.useQuery("get", "/api/account/subscriptions/stripe-health");

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

  const cancelDowngradeMutation = api.useMutation("post", "/api/account/subscriptions/cancel-downgrade", {
    onSuccess: () => {
      setIsCancelDowngradeDialogOpen(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your scheduled downgrade has been cancelled.`);
    }
  });

  const syncMutation = api.useMutation("post", "/api/account/subscriptions/sync", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/payment-history"] });
      toast.success(t`Subscription synced with Stripe.`);
    }
  });

  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const sessionId = urlParams.get("session_id");

    if (sessionId) {
      setIsProcessing(true);
      window.history.replaceState({}, "", window.location.pathname);
      checkoutSuccessMutation.mutate({ body: { sessionId } });
    }
  }, []);

  const isStripeConfigured = stripeHealth?.isConfigured ?? false;
  const currentPlan = subscription?.plan ?? SubscriptionPlan.Basis;
  const cancelAtPeriodEnd = subscription?.cancelAtPeriodEnd ?? false;
  const scheduledPlan = subscription?.scheduledPlan ?? null;
  const currentPeriodEnd = subscription?.currentPeriodEnd ?? null;
  const hasStripeCustomer = subscription?.hasStripeCustomer ?? false;
  const hasStripeSubscription = subscription?.hasStripeSubscription ?? false;
  const formattedPeriodEndLong = formatLongDate(currentPeriodEnd);

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

  if (!hasStripeSubscription) {
    const handleSubscribe = (plan: SubscriptionPlan) => {
      checkoutMutation.mutate({
        body: {
          plan,
          successUrl: `${window.location.origin}/account/subscription/?session_id={CHECKOUT_SESSION_ID}`,
          cancelUrl: `${window.location.origin}/account/subscription/`
        }
      });
    };

    return (
      <>
        <AppLayout
          variant="center"
          maxWidth="60rem"
          title={t`Subscription`}
          subtitle={t`Choose a plan to get started.`}
        >
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
                cancelAtPeriodEnd={false}
                scheduledPlan={null}
                isStripeConfigured={isStripeConfigured}
                onSubscribe={handleSubscribe}
                onUpgrade={() => {}}
                onDowngrade={() => {}}
                onReactivate={() => {}}
                onCancelDowngrade={() => {}}
                isPending={checkoutMutation.isPending}
                pendingPlan={checkoutMutation.isPending ? (checkoutMutation.variables?.body?.plan ?? null) : null}
                isCancelDowngradePending={false}
              />
            ))}
          </div>
          {hasStripeCustomer && (
            <Button
              variant="outline"
              className="mt-4 w-fit md:self-end"
              onClick={() => syncMutation.mutate({})}
              disabled={syncMutation.isPending}
            >
              <RefreshCwIcon className={`size-4 ${syncMutation.isPending ? "animate-spin" : ""}`} />
              {syncMutation.isPending ? t`Syncing...` : t`Sync with Stripe`}
            </Button>
          )}
        </AppLayout>

        <ProcessingPaymentModal isOpen={isProcessing} />
      </>
    );
  }

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="60rem"
        title={t`Subscription`}
        subtitle={t`Manage your subscription and billing.`}
      >
        <SubscriptionTabNavigation activeTab="overview" />

        {cancelAtPeriodEnd && (
          <div className="mb-6 flex items-center justify-between gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
            <div className="flex items-center gap-3">
              <AlertTriangleIcon className="size-4 shrink-0" />
              {formattedPeriodEndLong ? (
                <Trans>
                  Your {getPlanLabel(currentPlan)} subscription has been cancelled and will end on{" "}
                  {formattedPeriodEndLong}.
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
        )}

        {scheduledPlan && !cancelAtPeriodEnd && (
          <div className="mb-6 flex items-center justify-between gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
            <div className="flex items-center gap-3">
              <AlertTriangleIcon className="size-4 shrink-0" />
              {formattedPeriodEndLong ? (
                <Trans>
                  Your subscription will be downgraded to {getPlanLabel(scheduledPlan)} on {formattedPeriodEndLong}.
                </Trans>
              ) : (
                <Trans>
                  Your subscription will be downgraded to {getPlanLabel(scheduledPlan)} at the end of the current
                  billing period.
                </Trans>
              )}
            </div>
            <Button size="sm" className="shrink-0" onClick={() => setIsCancelDowngradeDialogOpen(true)}>
              <Trans>Cancel downgrade</Trans>
            </Button>
          </div>
        )}

        <div className="flex flex-col gap-4">
          <h3>
            <Trans>Current plan</Trans>
          </h3>
          <Separator />
          <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
            <div className="flex flex-col gap-2">
              <div className="flex flex-wrap items-center gap-3">
                <span className="font-medium">{getPlanLabel(currentPlan)}</span>
                {cancelAtPeriodEnd ? (
                  <Badge variant="destructive">
                    <Trans>Cancelling</Trans>
                  </Badge>
                ) : (
                  <Badge variant="default">
                    <Trans>Active</Trans>
                  </Badge>
                )}
              </div>
              {formattedPeriodEndLong && (
                <p className="text-muted-foreground text-sm">
                  {cancelAtPeriodEnd ? (
                    <Trans>Access until {formattedPeriodEndLong}</Trans>
                  ) : (
                    <Trans>Next billing date: {formattedPeriodEndLong}</Trans>
                  )}
                </p>
              )}
            </div>
            {hasStripeCustomer && (
              <Button
                variant="outline"
                className="w-fit"
                onClick={() => syncMutation.mutate({})}
                disabled={syncMutation.isPending}
              >
                <RefreshCwIcon className={`size-4 ${syncMutation.isPending ? "animate-spin" : ""}`} />
                {syncMutation.isPending ? t`Syncing...` : t`Sync with Stripe`}
              </Button>
            )}
          </div>
        </div>

        <div className="mt-8 flex flex-col gap-4">
          <h3>
            <Trans>Payment method</Trans>
          </h3>
          <Separator />
          <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
            <PaymentMethodDisplay paymentMethod={subscription?.paymentMethod} />
            <Button
              variant="outline"
              className="w-fit"
              onClick={() => setIsUpdatePaymentMethodOpen(true)}
              disabled={!isStripeConfigured}
            >
              <PencilIcon className="size-4" />
              <Trans>Update payment method</Trans>
            </Button>
          </div>
        </div>

        <div className="mt-8 flex flex-col gap-4">
          <h3>
            <Trans>Billing information</Trans>
          </h3>
          <Separator />
          <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
            <BillingInfoDisplay billingInfo={subscription?.billingInfo} />
            <Button
              variant="outline"
              className="w-fit shrink-0"
              onClick={() => setIsEditBillingInfoOpen(true)}
              disabled={!isStripeConfigured}
            >
              <PencilIcon className="size-4" />
              <Trans>Edit billing information</Trans>
            </Button>
          </div>
        </div>

        <div className="mt-8 flex flex-col gap-4">
          <h3>
            <Trans>Billing history</Trans>
          </h3>
          <Separator />
          <BillingHistoryTable />
        </div>
      </AppLayout>

      <ProcessingPaymentModal isOpen={isProcessing} />

      {scheduledPlan && (
        <CancelDowngradeDialog
          isOpen={isCancelDowngradeDialogOpen}
          onOpenChange={setIsCancelDowngradeDialogOpen}
          onConfirm={() => cancelDowngradeMutation.mutate({})}
          isPending={cancelDowngradeMutation.isPending}
          currentPlan={currentPlan}
          scheduledPlan={scheduledPlan}
          currentPeriodEnd={currentPeriodEnd}
        />
      )}

      <ReactivateConfirmationDialog
        isOpen={isReactivateDialogOpen}
        onOpenChange={setIsReactivateDialogOpen}
        onConfirm={() =>
          reactivateMutation.mutate({
            body: {
              plan: currentPlan,
              successUrl: `${window.location.origin}/account/subscription/?session_id={CHECKOUT_SESSION_ID}`,
              cancelUrl: `${window.location.origin}/account/subscription/`
            }
          })
        }
        isPending={reactivateMutation.isPending}
        currentPlan={currentPlan}
        targetPlan={currentPlan}
      />

      <EditBillingInfoDialog
        isOpen={isEditBillingInfoOpen}
        onOpenChange={setIsEditBillingInfoOpen}
        billingInfo={subscription?.billingInfo}
      />

      <UpdatePaymentMethodDialog isOpen={isUpdatePaymentMethodOpen} onOpenChange={setIsUpdatePaymentMethodOpen} />
    </>
  );
}
