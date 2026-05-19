import { t } from "@lingui/core/macro";
import { Toggle } from "@repo/ui/components/Toggle";
import { BoldIcon, ItalicIcon, UnderlineIcon } from "lucide-react";

export function TogglesPreview() {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <Toggle aria-label={t`Bold`}>
        <BoldIcon />
      </Toggle>
      <Toggle aria-label={t`Italic`}>
        <ItalicIcon />
      </Toggle>
      <Toggle aria-label={t`Underline`}>
        <UnderlineIcon />
      </Toggle>
      <Toggle variant="outline" aria-label={t`Bold`}>
        <BoldIcon />
      </Toggle>
    </div>
  );
}
