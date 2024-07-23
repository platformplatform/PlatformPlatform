import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/ui/layout/HorizontalHeroLayout";
import { LoginForm } from "@/shared/ui/auth/LoginForm";
import { ErrorMessage } from "@/shared/ui/auth/ErrorMessage";

export const Route = createFileRoute("/(auth)/login")({
  component: () => (
    <HorizontalHeroLayout>
      <LoginForm />
    </HorizontalHeroLayout>
  ),
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});
