import type { ReactNode } from "react";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";

import logoWrapUrl from "@/shared/images/logo-wrap.svg";

interface VerificationFooterProps {
  showRequestLink: boolean;
  isRateLimited: boolean;
  resendMutation: {
    mutate: (data: { params: { path: { id: string } } }) => void;
    isPending: boolean;
    error?: { errors?: Record<string, string[]> } | null;
  };
  resendPath: { id: string };
  backLink: ReactNode;
}

export function VerificationFooter({
  showRequestLink,
  isRateLimited,
  resendMutation,
  resendPath,
  backLink
}: VerificationFooterProps) {
  return (
    <div className="flex flex-col items-center gap-2 text-sm text-muted-foreground">
      <div className="text-center text-sm">
        <Trans>Can&apos;t find your code?</Trans>{" "}
        {!showRequestLink || isRateLimited ? (
          <Trans>Check your spam folder.</Trans>
        ) : (
          <Form
            onSubmit={(e) => {
              mutationSubmitter(resendMutation, { path: resendPath })(e);
            }}
            validationErrors={resendMutation.error?.errors}
            className="inline"
          >
            <Button type="submit" variant="link" disabled={resendMutation.isPending} className="h-auto p-0 text-sm">
              <Trans>Request a new code</Trans>
            </Button>
          </Form>
        )}
      </div>
      {backLink}
      <div className="mt-6 flex flex-col items-center gap-1">
        <span className="text-sm text-muted-foreground">
          <Trans>Powered by</Trans>
        </span>
        <Link href="https://github.com/platformplatform/PlatformPlatform" className="cursor-pointer">
          <img src={logoWrapUrl} alt={t`PlatformPlatform`} className="h-6 w-auto" />
        </Link>
      </div>
    </div>
  );
}
