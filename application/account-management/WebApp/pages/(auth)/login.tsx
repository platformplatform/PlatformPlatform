import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/ui/layout/HorizontalHeroLayout";
import { LoginForm } from "@/shared/ui/auth/LoginForm";

export const Route = createFileRoute("/(auth)/login")({
  component: () => (
    <HorizontalHeroLayout>
      <LoginForm />
    </HorizontalHeroLayout>
  )
});
