import { ErrorMessage } from "@/shared/components/ErrorMessage";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath, loginPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { Heading } from "@repo/ui/components/Heading";
import { Link } from "@repo/ui/components/Link";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { Navigate, createFileRoute } from "@tanstack/react-router";
import { DotIcon } from "lucide-react";
import { useState } from "react";
import { setSignupState } from "./-shared/signupState";

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

  const startSignupMutation = api.useMutation("post", "/api/account-management/signups/start");

  if (startSignupMutation.isSuccess) {
    const { emailConfirmationId, validForSeconds } = startSignupMutation.data;

    setSignupState({
      emailConfirmationId,
      email,
      expireAt: new Date(Date.now() + validForSeconds * 1000)
    });

    return <Navigate to="/signup/verify" />;
  }

  return (
    <Form
      onSubmit={mutationSubmitter(startSignupMutation)}
      validationErrors={startSignupMutation.error?.errors}
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
        autoFocus={true}
        isRequired={true}
        value={email}
        onChange={setEmail}
        autoComplete="email webauthn"
        placeholder={t`yourname@example.com`}
        className="flex w-full flex-col"
      />
      <Select
        name="region"
        selectedKey="europe"
        label={t`Region`}
        description={t`This is the region where your data is stored`}
        isRequired={true}
        className="flex w-full flex-col"
      >
        <SelectItem id="europe">
          <Trans>Europe</Trans>
        </SelectItem>
      </Select>
      <FormErrorMessage error={startSignupMutation.error} />
      <Button type="submit" isDisabled={startSignupMutation.isPending} className="mt-4 w-full text-center">
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
            <Trans>Privacy policies</Trans>
          </Link>
        </div>
      </div>
      <img src={poweredByUrl} alt={t`Powered by`} />
    </Form>
  );
}
