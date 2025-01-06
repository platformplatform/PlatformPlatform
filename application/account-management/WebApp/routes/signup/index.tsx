import { createFileRoute, Navigate } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import { DotIcon } from "lucide-react";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Heading } from "@repo/ui/components/Heading";
import { Link } from "@repo/ui/components/Link";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { DomainInputField } from "@repo/ui/components/DomainInputField";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { TextField } from "@repo/ui/components/TextField";
import { Form } from "@repo/ui/components/Form";
import { useActionState, useState } from "react";
import { api, useApi } from "@/shared/lib/api/client";
import { setSignupState } from "./-shared/signupState";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { loggedInPath, loginPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";

export const Route = createFileRoute("/signup/")({
  component: function SignupRoute() {
    const isAuthenticated = useIsAuthenticated();

    if (isAuthenticated) {
      return <Navigate to={loggedInPath} />;
    }

    return (
      <HorizontalHeroLayout>
        <StartSignupForm />
      </HorizontalHeroLayout>
    );
  },
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});

export function StartSignupForm() {
  const [email, setEmail] = useState("");

  const [{ success, errors, data, title, message }, action, isPending] = useActionState(
    api.actionPost("/api/account-management/signups/start"),
    { success: null }
  );

  const [subdomain, setSubdomain] = useState("");
  const { data: isSubdomainFree } = useApi(
    "/api/account-management/signups/is-subdomain-free",
    {
      params: {
        query: { Subdomain: subdomain }
      }
    },
    {
      autoFetch: subdomain.length > 3,
      debounceMs: 500
    }
  );

  if (success === true) {
    const { signupId, validForSeconds } = data;

    setSignupState({
      signupId,
      email,
      expireAt: new Date(Date.now() + validForSeconds * 1000)
    });

    return <Navigate to="/signup/verify" />;
  }

  return (
    <Form
      action={action}
      validationErrors={errors}
      validationBehavior="aria"
      className="flex w-full max-w-sm flex-col items-center gap-4 space-y-3 rounded-lg px-6 pt-8 pb-4"
    >
      <Link href="/">
        <img src={logoMarkUrl} className="h-12 w-12" alt={t`Logo`} />
      </Link>
      <Heading className="text-2xl">
        <Trans>Create your account</Trans>
      </Heading>
      <div className="text-center text-muted-foreground text-sm">
        <Trans>Sign up in seconds to start building on PlatformPlatform – just like thousands of others.</Trans>
      </div>
      <TextField
        name="email"
        type="email"
        label={t`Email`}
        autoFocus
        isRequired
        value={email}
        onChange={setEmail}
        autoComplete="email webauthn"
        placeholder={t`yourname@example.com`}
        className="flex w-full flex-col"
      />
      <DomainInputField
        name="subdomain"
        domain=".platformplatform.net"
        label={t`Subdomain`}
        placeholder={t`subdomain`}
        isRequired
        value={subdomain}
        onChange={setSubdomain}
        isSubdomainFree={isSubdomainFree}
        className="flex w-full flex-col"
      />
      <Select
        name="region"
        selectedKey="europe"
        label={t`Region`}
        description={t`This is the region where your data is stored`}
        isRequired
        className="flex w-full flex-col"
      >
        <SelectItem id="europe">
          <Trans>Europe</Trans>
        </SelectItem>
      </Select>
      <FormErrorMessage title={title} message={message} />
      <Button type="submit" isDisabled={isPending} className="mt-4 w-full text-center">
        <Trans>Create your account</Trans>
      </Button>
      <p className="text-muted-foreground text-xs">
        <Trans>Do you already have an account?</Trans>{" "}
        <Link href={loginPath}>
          <Trans>Log in</Trans>
        </Link>
      </p>
      <div className="text-muted-foreground text-sm">
        <Trans>By continuing, you accept our policies</Trans>
        <div className="flex items-center justify-center">
          <Link href="/">
            <Trans>Terms of use</Trans>
          </Link>
          <DotIcon className="h-4 w-4" />
          <Link href="/">
            <Trans>Privacy Policies</Trans>
          </Link>
        </div>
      </div>
      <img src={poweredByUrl} alt={t`Powered by`} />
    </Form>
  );
}
