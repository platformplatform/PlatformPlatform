import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { MultiSelect } from "@repo/ui/components/MultiSelect";
import {
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue
} from "@repo/ui/components/Select";
import { SelectField } from "@repo/ui/components/SelectField";
import { TimeZonePicker } from "@repo/ui/components/TimeZonePicker";
import { TrendingUpIcon, UtensilsCrossedIcon } from "lucide-react";
import { useState } from "react";

import type { ControlRowDerivedProps } from "./controlRowTypes";

import { ComboboxFields } from "./ComboboxFields";
import { tooltips } from "./controlTooltips";
import { useChartItems, useChartSelectItems, useRecipeGroups } from "./sampleRecipeData";

export function SelectAndComboboxFields({
  suffix,
  label,
  tooltip,
  disabled,
  readOnly,
  showIcon,
  hasValues,
  placeholders,
  errorMessage
}: ControlRowDerivedProps) {
  const chartItems = useChartItems();
  const [selectedColor, setSelectedColor] = useState<string>(hasValues ? "bar" : "");
  const [selectedCharts, setSelectedCharts] = useState<string[]>(hasValues ? ["bar", "pie"] : []);
  const recipeGroups = useRecipeGroups();
  const allRecipes = recipeGroups.flatMap((group) => group.recipes);
  const [recipe, setRecipe] = useState<string | null>(hasValues ? "carbonara" : null);
  const browserTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  const [timeZone, setTimeZone] = useState<string | null>(hasValues ? browserTimeZone : null);
  const chartSelectItems = useChartSelectItems();
  const selectedChartIcon = chartSelectItems.find((i) => i.value === selectedColor)?.icon;
  const hasCharts = selectedCharts.length > 0;

  return (
    <>
      <SelectField
        label={label ? t`Select` : undefined}
        tooltip={tooltip ? tooltips.select : undefined}
        name={`select-${suffix}`}
        items={chartSelectItems}
        value={selectedColor || null}
        onValueChange={(value) => setSelectedColor(value ?? "")}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      >
        <SelectTrigger>
          {showIcon && (selectedChartIcon ? selectedChartIcon : placeholders ? <TrendingUpIcon /> : null)}
          <SelectValue placeholder={placeholders ? t`Pick a chart` : undefined} />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={null} className="text-muted-foreground">
            <Trans>None</Trans>
          </SelectItem>
          {chartSelectItems.map((item) => (
            <SelectItem key={item.value} value={item.value}>
              {showIcon ? item.icon : null}
              {item.label}
            </SelectItem>
          ))}
        </SelectContent>
      </SelectField>
      <SelectField
        label={label ? t`Select with groups` : undefined}
        tooltip={tooltip ? tooltips.selectWithGroups : undefined}
        name={`recipe-${suffix}`}
        items={allRecipes}
        value={recipe}
        onValueChange={(value) => setRecipe(value ?? null)}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      >
        <SelectTrigger>
          {showIcon && (recipe || placeholders) ? <UtensilsCrossedIcon /> : null}
          <SelectValue placeholder={placeholders ? t`Pick a recipe` : undefined} />
        </SelectTrigger>
        <SelectContent>
          {recipeGroups.map(({ cuisine, recipes }) => (
            <SelectGroup key={cuisine} className="p-0">
              <SelectLabel className="sticky -top-1 z-10 -mx-1 bg-muted px-3 pt-2.5 pb-1.5 font-semibold text-foreground">
                {cuisine}
              </SelectLabel>
              {recipes.map((item) => (
                <SelectItem key={item.value} value={item.value}>
                  {item.label}
                </SelectItem>
              ))}
            </SelectGroup>
          ))}
        </SelectContent>
      </SelectField>
      <TimeZonePicker
        label={label ? t`Time zone` : undefined}
        tooltip={tooltip ? tooltips.timeZonePicker : undefined}
        name={`timezone-${suffix}`}
        placeholder={placeholders ? undefined : ""}
        startIcon={showIcon ? undefined : null}
        value={timeZone}
        onValueChange={setTimeZone}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
      <MultiSelect
        label={label ? t`Multi select` : undefined}
        tooltip={tooltip ? tooltips.multiSelect : undefined}
        name={`multi-${suffix}`}
        placeholder={placeholders ? t`Select charts` : undefined}
        startIcon={showIcon && (hasCharts || placeholders) ? <TrendingUpIcon /> : undefined}
        items={showIcon ? chartItems : chartItems.map(({ icon: _, ...rest }) => rest)}
        value={selectedCharts}
        onChange={setSelectedCharts}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
      <ComboboxFields
        suffix={suffix}
        label={label}
        tooltip={tooltip}
        disabled={disabled}
        readOnly={readOnly}
        showIcon={showIcon}
        hasValues={hasValues}
        placeholders={placeholders}
        errorMessage={errorMessage}
        chartItems={chartItems}
      />
    </>
  );
}
