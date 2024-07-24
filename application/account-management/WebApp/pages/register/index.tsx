import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/ui/layout/HorizontalHeroLayout";
import { StartAccountRegistrationForm } from "@/shared/ui/auth/StartAccountRegistrationForm";
import { ErrorMessage } from "@/shared/ui/auth/ErrorMessage";

export const Route = createFileRoute("/register/")({
  component: () => (
    <HorizontalHeroLayout>
      <StartAccountRegistrationForm />
    </HorizontalHeroLayout>
  ),
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});
