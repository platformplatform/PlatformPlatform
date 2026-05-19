import { Trans } from "@lingui/react/macro";
import { Link } from "@repo/ui/components/Link";
import { ExternalLinkIcon } from "lucide-react";

export function LinkPreview() {
  return (
    <>
      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Link variants</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-4">
          <Link href="/components">
            <Trans>Primary</Trans>
          </Link>
          <Link href="/components" variant="secondary">
            <Trans>Secondary</Trans>
          </Link>
          <Link href="/components" variant="destructive">
            <Trans>Destructive</Trans>
          </Link>
          <Link href="/components" variant="ghost">
            <Trans>Ghost</Trans>
          </Link>
          <Link href="https://example.com" variant="primary">
            <Trans>External</Trans>
            <ExternalLinkIcon />
          </Link>
          <Link href="/components" disabled>
            <Trans>Disabled</Trans>
          </Link>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Link underline</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-4">
          <Link href="/components" underline={true}>
            <Trans>Always underlined</Trans>
          </Link>
          <Link href="/components" underline="hover">
            <Trans>Underline on hover</Trans>
          </Link>
          <Link href="/components" underline={false}>
            <Trans>Never underlined</Trans>
          </Link>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Link sizes</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-4">
          <Link href="/components" size="sm">
            <Trans>Small</Trans>
          </Link>
          <Link href="/components" size="md">
            <Trans>Medium</Trans>
          </Link>
          <Link href="/components" size="lg">
            <Trans>Large</Trans>
          </Link>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Button-styled links</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-3">
          <Link href="/components" variant="button-primary">
            <Trans>Primary</Trans>
          </Link>
          <Link href="/components" variant="button-secondary">
            <Trans>Secondary</Trans>
          </Link>
          <Link href="/components" variant="button-destructive">
            <Trans>Destructive</Trans>
          </Link>
        </div>
      </div>
    </>
  );
}
