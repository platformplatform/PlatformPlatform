import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { requirePermission } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useFormatLongDate } from "@repo/ui/hooks/useSmartDate";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { AlertTriangleIcon, PencilIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, SubscriptionPlan } from "@/shared/lib/api/client";
import { BillingHistoryTable } from "./-components/BillingHistoryTable";
import { BillingInfoDisplay } from "./-components/BillingInfoDisplay";
import { CancelDowngradeDialog } from "./-components/CancelDowngradeDialog";
import { CheckoutDialog } from "./-components/CheckoutDialog";
import { EditBillingInfoDialog } from "./-components/EditBillingInfoDialog";
import { PaymentMethodDisplay } from "./-components/PaymentMethodDisplay";
import { PlanCard } from "./-components/PlanCard";
import { ProcessingPaymentModal } from "./-components/ProcessingPaymentModal";
import { ReactivateConfirmationDialog } from "./-components/ReactivateConfirmationDialog";
import { SubscriptionTabNavigation } from "./-components/SubscriptionTabNavigation";
import { UpdatePaymentMethodDialog } from "./-components/UpdatePaymentMethodDialog";
import { useSubscriptionPolling } from "./-components/useSubscriptionPolling";

export const Route = createFileRoute("/account/subscription/")({
  staticData: { trackingTitle: "Subscription" },
  beforeLoad: () => requirePermission({ allowedRoles: ["Owner"] }),
  component: SubscriptionPage
});

function SubscriptionPage() {
  const navigate = useNavigate();
  const formatLongDate = useFormatLongDate();
  const queryClient = useQueryClient();
  const { isPolling, startPolling, subscription } = useSubscriptionPolling();
  const [isProcessing, setIsProcessing] = useState(false);
  const [isCancelDowngradeDialogOpen, setIsCancelDowngradeDialogOpen] = useState(false);
  const [isReactivateDialogOpen, setIsReactivateDialogOpen] = useState(false);
  const [isEditBillingInfoOpen, setIsEditBillingInfoOpen] = useState(false);
  const [isUpdatePaymentMethodOpen, setIsUpdatePaymentMethodOpen] = useState(false);
  const [isCheckoutDialogOpen, setIsCheckoutDialogOpen] = useState(false);
  const [checkoutPlan, setCheckoutPlan] = useState<SubscriptionPlan>(SubscriptionPlan.Basis);
  const [pendingCheckoutPlan, setPendingCheckoutPlan] = useState<SubscriptionPlan | null>(null);
  const [reactivateClientSecret, setReactivateClientSecret] = useState<string | undefined>();
  const [reactivatePublishableKey, setReactivatePublishableKey] = useState<string | undefined>();

  const { data: stripeHealth } = api.useQuery("get", "/api/account/subscriptions/stripe-health");
  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");

  const checkoutSuccessMutation = api.useMutation("post", "/api/account/subscriptions/checkout-success", {
    onSuccess: () => {
      setIsProcessing(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your subscription has been activated.`);
    }
  });

  const reactivateMutation = api.useMutation("post", "/api/account/subscriptions/reactivate", {
    onSuccess: (data) => {
      if (data.clientSecret && data.publishableKey) {
        setIsReactivateDialogOpen(false);
        setReactivateClientSecret(data.clientSecret);
        setReactivatePublishableKey(data.publishableKey);
        setPendingCheckoutPlan(currentPlan);
        setIsEditBillingInfoOpen(true);
      } else {
        startPolling({
          check: (subscription) => subscription.cancelAtPeriodEnd === false,
          successMessage: t`Your subscription has been reactivated.`,
          onComplete: () => setIsReactivateDialogOpen(false)
        });
      }
    }
  });

  const cancelDowngradeMutation = api.useMutation("post", "/api/account/subscriptions/cancel-downgrade", {
    onSuccess: () => {
      startPolling({
        check: (subscription) => subscription.scheduledPlan == null,
        successMessage: t`Your scheduled downgrade has been cancelled.`,
        onComplete: () => setIsCancelDowngradeDialogOpen(false)
      });
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
  const hasStripeSubscription = subscription?.hasStripeSubscription ?? false;
  const formattedPeriodEndLong = formatLongDate(currentPeriodEnd);

  const billingInfo = subscription?.billingInfo;

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

  const handleBillingInfoSuccess = () => {
    if (pendingCheckoutPlan == null) {
      return;
    }
    setCheckoutPlan(pendingCheckoutPlan);
    setPendingCheckoutPlan(null);
    setIsCheckoutDialogOpen(true);
  };

  const handleSubscribe = (plan: SubscriptionPlan) => {
    setPendingCheckoutPlan(plan);
    setIsEditBillingInfoOpen(true);
  };

  return (
    <>
      {hasStripeSubscription ? (
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
            <div className="flex items-center justify-between gap-4">
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
              <Tooltip>
                <TooltipTrigger
                  render={
                    <Button
                      variant="outline"
                      size="sm"
                      className="shrink-0 gap-1.5"
                      aria-label={t`Change plan`}
                      onClick={() => navigate({ to: "/account/subscription/plans" })}
                    >
                      <PencilIcon className="size-4" />
                      <span className="hidden sm:inline" aria-hidden="true">
                        <Trans>Change</Trans>
                      </span>
                    </Button>
                  }
                />
                <TooltipContent className="sm:hidden">
                  <Trans>Change plan</Trans>
                </TooltipContent>
              </Tooltip>
            </div>
          </div>

          <div className="mt-8 flex flex-col gap-4">
            <h3>
              <Trans>Payment method</Trans>
            </h3>
            <Separator />
            <div className="flex items-center justify-between gap-4">
              <PaymentMethodDisplay paymentMethod={subscription?.paymentMethod} />
              <Tooltip>
                <TooltipTrigger
                  render={
                    <Button
                      variant="outline"
                      size="sm"
                      className="shrink-0 gap-1.5"
                      aria-label={t`Update payment method`}
                      onClick={() => setIsUpdatePaymentMethodOpen(true)}
                      disabled={!isStripeConfigured}
                    >
                      <PencilIcon className="size-4" />
                      <span className="hidden sm:inline" aria-hidden="true">
                        <Trans>Update</Trans>
                      </span>
                    </Button>
                  }
                />
                <TooltipContent className="sm:hidden">
                  <Trans>Update payment method</Trans>
                </TooltipContent>
              </Tooltip>
            </div>
          </div>

          <div className="mt-8 flex flex-col gap-4">
            <h3>
              <Trans>Billing information</Trans>
            </h3>
            <Separator />
            <div className="flex items-start justify-between gap-4">
              <BillingInfoDisplay billingInfo={subscription?.billingInfo} />
              <Tooltip>
                <TooltipTrigger
                  render={
                    <Button
                      variant="outline"
                      size="sm"
                      className="shrink-0 gap-1.5"
                      aria-label={t`Edit billing information`}
                      onClick={() => setIsEditBillingInfoOpen(true)}
                      disabled={!isStripeConfigured}
                    >
                      <PencilIcon className="size-4" />
                      <span className="hidden sm:inline" aria-hidden="true">
                        <Trans>Edit</Trans>
                      </span>
                    </Button>
                  }
                />
                <TooltipContent className="sm:hidden">
                  <Trans>Edit billing information</Trans>
                </TooltipContent>
              </Tooltip>
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
      ) : (
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
                isPending={false}
                pendingPlan={null}
                isCancelDowngradePending={false}
              />
            ))}
          </div>
        </AppLayout>
      )}

      <ProcessingPaymentModal isOpen={isProcessing} />

      {scheduledPlan && (
        <CancelDowngradeDialog
          isOpen={isCancelDowngradeDialogOpen}
          onOpenChange={setIsCancelDowngradeDialogOpen}
          onConfirm={() => cancelDowngradeMutation.mutate({})}
          isPending={cancelDowngradeMutation.isPending || isPolling}
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
              returnUrl: `${window.location.origin}/account/subscription/?session_id={CHECKOUT_SESSION_ID}`
            }
          })
        }
        isPending={reactivateMutation.isPending || isPolling}
        currentPlan={currentPlan}
        targetPlan={currentPlan}
      />

      <EditBillingInfoDialog
        isOpen={isEditBillingInfoOpen}
        onOpenChange={setIsEditBillingInfoOpen}
        billingInfo={billingInfo}
        tenantName={tenant?.name ?? ""}
        onSuccess={handleBillingInfoSuccess}
        submitLabel={pendingCheckoutPlan != null ? t`Next` : undefined}
        pendingLabel={pendingCheckoutPlan != null ? t`Saving...` : undefined}
      />

      <UpdatePaymentMethodDialog isOpen={isUpdatePaymentMethodOpen} onOpenChange={setIsUpdatePaymentMethodOpen} />

      <CheckoutDialog
        isOpen={isCheckoutDialogOpen}
        onOpenChange={(open) => {
          setIsCheckoutDialogOpen(open);
          if (!open) {
            setReactivateClientSecret(undefined);
            setReactivatePublishableKey(undefined);
          }
        }}
        plan={checkoutPlan}
        prefetchedClientSecret={reactivateClientSecret}
        prefetchedPublishableKey={reactivatePublishableKey}
      />
    </>
  );
}
