import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Spinner } from "@repo/ui/components/Spinner";

export function SpinnerPreview() {
  return (
    <section className="flex flex-col gap-3">
      <h3>
        <Trans>Spinner</Trans>
      </h3>
      <p className="text-sm text-muted-foreground">
        <Trans>
          Indeterminate loading indicator. Use Progress when you can show how much work remains; reach for Spinner when
          you can't.
        </Trans>
      </p>
      <div className="flex flex-wrap items-center gap-6">
        <Spinner className="size-4" />
        <Spinner className="size-6 text-muted-foreground" />
        <Spinner className="size-8 text-primary" />
        <Button disabled>
          <Spinner />
          <Trans>Saving...</Trans>
        </Button>
      </div>
    </section>
  );
}
