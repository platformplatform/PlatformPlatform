import { Trans } from "@lingui/react/macro";
import { Progress } from "@repo/ui/components/Progress";

export function ProgressPreview() {
  return (
    <section className="flex flex-col gap-3">
      <h3>
        <Trans>Progress</Trans>
      </h3>
      <p className="text-sm text-muted-foreground">
        <Trans>
          Linear progress bar for determinate, foreground tasks like uploads or onboarding completion. Use Spinner for
          indeterminate or background loading.
        </Trans>
      </p>
      <div className="flex max-w-md flex-col gap-4">
        <div className="flex items-center gap-3">
          <span className="w-12 text-xs text-muted-foreground tabular-nums">100%</span>
          <Progress value={100} variant="success" className="flex-1" />
        </div>
        <div className="flex items-center gap-3">
          <span className="w-12 text-xs text-muted-foreground tabular-nums">75%</span>
          <Progress value={75} className="flex-1" />
        </div>
        <div className="flex items-center gap-3">
          <span className="w-12 text-xs text-muted-foreground tabular-nums">40%</span>
          <Progress value={40} variant="warning" className="flex-1" />
        </div>
        <div className="flex items-center gap-3">
          <span className="w-12 text-xs text-muted-foreground tabular-nums">15%</span>
          <Progress value={15} variant="destructive" className="flex-1" />
        </div>
        <div className="flex items-center gap-3">
          <span className="w-12 text-xs text-muted-foreground tabular-nums">5%</span>
          <Progress value={5} variant="destructive" className="flex-1" />
        </div>
        <div className="flex items-center gap-3">
          <span className="w-12 text-xs text-muted-foreground tabular-nums">—</span>
          <Progress value={0} variant="neutral" className="flex-1" />
        </div>
      </div>
    </section>
  );
}
