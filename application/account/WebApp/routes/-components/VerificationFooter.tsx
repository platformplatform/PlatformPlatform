import type { ReactNode } from "react";

import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";

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
            <Button type="submit" variant="link" isPending={resendMutation.isPending} className="h-auto p-0 text-sm">
              <Trans>Request a new code</Trans>
            </Button>
          </Form>
        )}
      </div>
      {backLink}
    </div>
  );
}
