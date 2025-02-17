import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Content, Heading, IllustratedMessage } from "@repo/ui/components/IllustratedMessage";
import ErrorIllustration from "@spectrum-icons/illustrations/Error";
import type { ErrorComponentProps } from "@tanstack/react-router";
import { useEffect } from "react";

export function ErrorMessage({ error, reset }: Readonly<ErrorComponentProps>) {
  useEffect(() => {
    // Log the error to an error reporting service
    console.error(error);
  }, [error]);

  return (
    <IllustratedMessage>
      <ErrorIllustration />
      <Heading>
        <Trans>Error: Something went wrong!</Trans>
      </Heading>
      <Content>
        <Trans>An error occurred while processing your request. {error.message}</Trans>
      </Content>
      <Button type="button" onPress={reset}>
        <Trans>Try again</Trans>
      </Button>
    </IllustratedMessage>
  );
}
