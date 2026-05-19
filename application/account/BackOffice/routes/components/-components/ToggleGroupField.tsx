import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Field, FieldLabel } from "@repo/ui/components/Field";
import { LabelWithTooltip } from "@repo/ui/components/LabelWithTooltip";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { BoldIcon, ItalicIcon, UnderlineIcon } from "lucide-react";
import { useRef, useState } from "react";

import { tooltips } from "./controlTooltips";

interface ToggleGroupFieldProps {
  label?: boolean;
  tooltip?: boolean;
  hasValues?: boolean;
  disabled?: boolean;
  readOnly?: boolean;
}

function focusToggleItem(ref: React.RefObject<HTMLDivElement | null>, isDisabled?: boolean) {
  if (isDisabled) return;
  const group = ref.current;
  if (!group) return;
  const active = group.querySelector<HTMLElement>("[data-slot=toggle-group-item][data-pressed]");
  const target = active ?? group.querySelector<HTMLElement>("[data-slot=toggle-group-item]");
  if (!target) return;
  target.setAttribute("data-label-focus", "");
  target.addEventListener("blur", () => target.removeAttribute("data-label-focus"), { once: true });
  target.focus({ preventScroll: true });
}

export function ToggleGroupField({ label, tooltip, hasValues, disabled, readOnly }: Readonly<ToggleGroupFieldProps>) {
  const toggleGroupRef = useRef<HTMLDivElement>(null);
  const focusToggle = () => focusToggleItem(toggleGroupRef, disabled);
  const [toggleValues, setToggleValues] = useState<string[]>(hasValues ? ["bold"] : []);

  return (
    <Field ref={toggleGroupRef}>
      {label && (
        <FieldLabel
          onClick={focusToggle}
          onKeyDown={(event) => {
            if (event.key === "Enter" || event.key === " ") {
              event.preventDefault();
              focusToggle();
            }
          }}
        >
          {tooltip ? (
            <LabelWithTooltip tooltip={tooltips.toggleGroup}>
              <Trans>Toggle group</Trans>
            </LabelWithTooltip>
          ) : (
            <Trans>Toggle group</Trans>
          )}
        </FieldLabel>
      )}
      <ToggleGroup
        variant="outline"
        value={toggleValues}
        onValueChange={setToggleValues}
        disabled={disabled}
        readOnly={readOnly}
      >
        <ToggleGroupItem value="bold" aria-label={t`Toggle bold`}>
          <BoldIcon />
        </ToggleGroupItem>
        <ToggleGroupItem value="italic" aria-label={t`Toggle italic`}>
          <ItalicIcon />
        </ToggleGroupItem>
        <ToggleGroupItem value="underline" aria-label={t`Toggle underline`}>
          <UnderlineIcon />
        </ToggleGroupItem>
      </ToggleGroup>
    </Field>
  );
}
