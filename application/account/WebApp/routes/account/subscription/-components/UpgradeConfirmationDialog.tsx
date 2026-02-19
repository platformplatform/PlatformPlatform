import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { Separator } from "@repo/ui/components/Separator";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import type { components } from "@/shared/lib/api/api.generated";
import { api, type SubscriptionPlan } from "@/shared/lib/api/client";
import { BillingInfoDisplay } from "./BillingInfoDisplay";
import { PaymentMethodDisplay } from "./PaymentMethodDisplay";
import { getFormattedPrice, getPlanDetails } from "./PlanCard";

type BillingInfo = components["schemas"]["BillingInfo"];
type PaymentMethod = components["schemas"]["PaymentMethod"];

type UpgradeConfirmationDialogProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onConfirm: () => void;
  isPending: boolean;
  targetPlan: SubscriptionPlan;
  billingInfo: BillingInfo | null | undefined;
  paymentMethod: PaymentMethod | null | undefined;
};

export function UpgradeConfirmationDialog({
  isOpen,
  onOpenChange,
  onConfirm,
  isPending,
  targetPlan,
  billingInfo,
  paymentMethod
}: Readonly<UpgradeConfirmationDialogProps>) {
  const { data: preview, isLoading: isPreviewLoading } = api.useQuery(
    "get",
    "/api/account/subscriptions/upgrade-preview",
    { params: { query: { NewPlan: targetPlan } } },
    { enabled: isOpen && !isPending }
  );

  const { data: pricingCatalog } = api.useQuery("get", "/api/account/subscriptions/pricing-catalog");
  const targetPlanDetails = getPlanDetails(targetPlan);
  const targetFormattedPrice = getFormattedPrice(targetPlan, pricingCatalog?.plans);

  return (
    <Dialog
      open={isOpen}
      onOpenChange={onOpenChange}
      disablePointerDismissal={true}
      trackingTitle="Upgrade subscription"
    >
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>{t`Upgrade to ${targetPlanDetails.name}`}</DialogTitle>
        </DialogHeader>
        <DialogBody>
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <span className="font-medium text-sm">
                <Trans>Bill to</Trans>
              </span>
              <BillingInfoDisplay billingInfo={billingInfo} />
            </div>

            <Separator />

            <div className="flex flex-col gap-2">
              <span className="font-medium text-sm">
                <Trans>Payment method</Trans>
              </span>
              <PaymentMethodDisplay paymentMethod={paymentMethod} />
            </div>

            <Separator />

            <div className="flex flex-col gap-2">
              {isPreviewLoading || preview == null ? (
                <div className="flex flex-col gap-2">
                  <div className="flex items-center justify-between">
                    <Skeleton className="h-4 w-[10rem]" />
                    <Skeleton className="h-4 w-[4rem]" />
                  </div>
                  <div className="flex items-center justify-between">
                    <Skeleton className="h-4 w-[12rem]" />
                    <Skeleton className="h-4 w-[4rem]" />
                  </div>
                  <Separator />
                  <div className="flex items-center justify-between">
                    <Skeleton className="h-5 w-[6rem]" />
                    <Skeleton className="h-5 w-[5rem]" />
                  </div>
                </div>
              ) : (
                <div className="flex flex-col gap-2">
                  {preview.lineItems.map((item, index) => (
                    <div key={index} className="flex items-baseline justify-between gap-4 text-sm">
                      <span className="min-w-0 text-muted-foreground">{item.description}</span>
                      <span className="shrink-0 whitespace-nowrap text-muted-foreground tabular-nums">
                        {formatCurrency(item.amount, item.currency)}
                      </span>
                    </div>
                  ))}
                  <Separator />
                  <div className="flex items-baseline justify-between gap-4 font-medium">
                    <span>
                      <Trans>Total</Trans>
                    </span>
                    <span className="shrink-0 whitespace-nowrap text-lg tabular-nums">
                      {formatCurrency(preview.totalAmount, preview.currency)}
                    </span>
                  </div>
                  <p className="text-muted-foreground text-xs">
                    <Trans>Estimated next bill: {targetFormattedPrice}</Trans>
                  </p>
                </div>
              )}
            </div>
          </div>
        </DialogBody>
        <DialogFooter>
          <DialogClose render={<Button type="reset" variant="secondary" disabled={isPending} />}>
            <Trans>Cancel</Trans>
          </DialogClose>
          <Button onClick={onConfirm} disabled={isPending || isPreviewLoading}>
            {isPending ? <Trans>Processing...</Trans> : t`Pay and upgrade`}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
