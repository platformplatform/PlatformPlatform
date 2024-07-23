import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/ui/layout/HorizontalHeroLayout";
import { StartAccountRegistrationForm } from "@/shared/ui/auth/StartAccountRegistrationForm";

export const Route = createFileRoute("/(auth)/register")({
  component: () => (
    <HorizontalHeroLayout>
      <StartAccountRegistrationForm />
    </HorizontalHeroLayout>
  )
});
