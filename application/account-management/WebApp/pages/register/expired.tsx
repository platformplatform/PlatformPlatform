import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/ui/layout/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/ui/auth/ErrorMessage";
import { VerificationCodeExpiredMessage } from "@/shared/ui/auth/VerificationCodeExpiredMessage";

export const Route = createFileRoute("/register/expired")({
  component: () => (
    <HorizontalHeroLayout>
      <VerificationCodeExpiredMessage />
    </HorizontalHeroLayout>
  ),
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});
