import type { DateRangeValue } from "@repo/ui/components/DateRangePicker";

import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { DatePicker } from "@repo/ui/components/DatePicker";
import { DateRangePicker } from "@repo/ui/components/DateRangePicker";
import { DialogBody, DialogFooter, DialogForm } from "@repo/ui/components/Dialog";
import { TextAreaField } from "@repo/ui/components/TextAreaField";
import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react";

interface CookingDetailsStepProps {
  simulateErrors: boolean;
  firstCooked: string | undefined;
  onFirstCookedChange: (value: string) => void;
  mealPlan: DateRangeValue | null;
  onMealPlanChange: (value: DateRangeValue | null) => void;
  onBack: () => void;
  onNext: () => void;
  onChange: () => void;
}

export function CookingDetailsStep({
  simulateErrors,
  firstCooked,
  onFirstCookedChange,
  mealPlan,
  onMealPlanChange,
  onBack,
  onNext,
  onChange
}: Readonly<CookingDetailsStepProps>) {
  const { i18n } = useLingui();

  return (
    <DialogForm>
      <DialogBody>
        <div className="grid grid-cols-2 gap-4">
          <DatePicker
            name="firstCooked"
            label={t`First cooked`}
            placeholder={t`Pick a date`}
            value={firstCooked}
            errorMessage={simulateErrors ? t`Date must be in the past` : undefined}
            onChange={(v) => {
              onFirstCookedChange(v);
              onChange();
            }}
            locale={i18n.locale}
          />
          <DateRangePicker
            name="mealPlan"
            label={t`Meal plan period`}
            value={mealPlan}
            onChange={(v) => {
              onMealPlanChange(v);
              onChange();
            }}
          />
          <TextAreaField
            autoFocus
            name="cookingNotes"
            label={t`Cooking notes`}
            defaultValue="Family favorite since childhood. Best served with fresh basil."
            placeholder={t`Add notes about this recipe`}
            className="col-span-2"
            onChange={onChange}
          />
        </div>
      </DialogBody>
      <DialogFooter>
        <Button type="button" variant="secondary" onClick={onBack}>
          <ChevronLeftIcon />
          <Trans>Back</Trans>
        </Button>
        <Button type="button" onClick={onNext}>
          <Trans>Next</Trans>
          <ChevronRightIcon />
        </Button>
      </DialogFooter>
    </DialogForm>
  );
}
