import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/ui/layout/HorizontalHeroLayout";
import { CompleteAccountRegistrationForm } from "@/shared/ui/auth/CompleteAccountRegistrationForm";
import { ErrorMessage } from "@/shared/ui/auth/ErrorMessage";

export const Route = createFileRoute("/(auth)/register/verify")({
  component: () => (
    <HorizontalHeroLayout>
      <CompleteAccountRegistrationForm />
    </HorizontalHeroLayout>
  ),
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});
