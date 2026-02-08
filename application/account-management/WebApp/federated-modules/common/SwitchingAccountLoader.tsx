import { Trans } from "@lingui/react/macro";
import { Loader2 } from "lucide-react";

export function SwitchingAccountLoader() {
  return (
    <div className="fixed inset-0 z-[99] flex items-center justify-center bg-black/50">
      <div className="flex flex-col items-center gap-4 rounded-lg bg-background p-6">
        <Loader2 className="size-8 animate-spin text-primary" />
        <p className="text-sm">
          <Trans>Switching account...</Trans>
        </p>
      </div>
    </div>
  );
}
