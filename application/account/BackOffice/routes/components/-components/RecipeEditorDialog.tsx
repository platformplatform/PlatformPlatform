import type { DateRangeValue } from "@repo/ui/components/DateRangePicker";

import { t } from "@lingui/core/macro";
import { DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@repo/ui/components/Dialog";
import { DirtyDialog } from "@repo/ui/components/DirtyDialog";
import { useDialogSetDirty } from "@repo/ui/components/DirtyDialogContext";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";

import { CategorizationStep } from "./CategorizationStep";
import { CookingDetailsStep } from "./CookingDetailsStep";
import { type DialogSize, getDialogSizeClassName } from "./dialogSize";
import { RecipeInfoStep } from "./RecipeInfoStep";

const TOTAL_STEPS = 3;

function StepIndicator({ current, total }: Readonly<{ current: number; total: number }>) {
  return (
    <div className="flex items-center gap-1.5">
      {Array.from({ length: total }, (_, i) => (
        <div
          key={i}
          className={`h-1.5 rounded-full transition-all ${i <= current ? "w-6 bg-primary" : "w-4 bg-muted"}`}
        />
      ))}
      <span className="ml-1 text-xs text-muted-foreground">
        {current + 1} / {total}
      </span>
    </div>
  );
}

const stepTitles = () => [t`Recipe info`, t`Cooking details`, t`Categorization`];
const stepDescriptions = () => [
  t`Basic recipe information and cover photo.`,
  t`Dates and personal notes about this recipe.`,
  t`Set the recipe's difficulty and favorite status.`
];

export interface RecipeEditorDialogProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  dirtyDialog: boolean;
  showToasts: boolean;
  simulateErrors: boolean;
  size: DialogSize;
}

export function RecipeEditorDialog({
  isOpen,
  onOpenChange,
  dirtyDialog,
  showToasts,
  simulateErrors,
  size
}: Readonly<RecipeEditorDialogProps>) {
  const handleClose = () => onOpenChange(false);
  return (
    <DirtyDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Edit recipe">
      <DialogContent className={getDialogSizeClassName(size)}>
        <RecipeEditorDialogBody
          onClose={handleClose}
          dirtyDialog={dirtyDialog}
          showToasts={showToasts}
          simulateErrors={simulateErrors}
        />
      </DialogContent>
    </DirtyDialog>
  );
}

function RecipeEditorDialogBody({
  onClose,
  dirtyDialog,
  showToasts,
  simulateErrors
}: {
  onClose: () => void;
  dirtyDialog: boolean;
  showToasts: boolean;
  simulateErrors: boolean;
}) {
  const setDirty = useDialogSetDirty();
  const [step, setStep] = useState(0);
  const [firstCooked, setFirstCooked] = useState<string | undefined>(undefined);
  const [mealPlan, setMealPlan] = useState<DateRangeValue | null>(null);

  const mutation = useMutation({
    mutationFn: async (_data: { body?: unknown }) => {
      await new Promise<void>((resolve) => setTimeout(resolve, 500));
    },
    onSuccess: () => {
      if (showToasts) toast.success(t`Recipe saved`);
      onClose();
    }
  });

  const markDirty = () => {
    if (dirtyDialog) setDirty(true);
  };

  const titles = stepTitles();
  const descriptions = stepDescriptions();

  return (
    <>
      <DialogHeader>
        <StepIndicator current={step} total={TOTAL_STEPS} />
        <DialogTitle>{titles[step]}</DialogTitle>
        <DialogDescription>{descriptions[step]}</DialogDescription>
      </DialogHeader>
      {step === 0 && (
        <RecipeInfoStep
          simulateErrors={simulateErrors}
          onNext={() => setStep(1)}
          onCancel={onClose}
          onChange={markDirty}
        />
      )}
      {step === 1 && (
        <CookingDetailsStep
          simulateErrors={simulateErrors}
          firstCooked={firstCooked}
          onFirstCookedChange={setFirstCooked}
          mealPlan={mealPlan}
          onMealPlanChange={setMealPlan}
          onBack={() => setStep(0)}
          onNext={() => setStep(2)}
          onChange={markDirty}
        />
      )}
      {step === 2 && <CategorizationStep mutation={mutation} onBack={() => setStep(1)} onChange={markDirty} />}
    </>
  );
}
