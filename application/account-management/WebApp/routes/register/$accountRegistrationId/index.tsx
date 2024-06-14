import { z } from "zod";
import { createFileRoute } from "@tanstack/react-router";
import { CompleteAccountRegistrationForm } from "../-components/CompleteAccountRegistrationForm";
import { ErrorPage } from "./-components/ErrorPage";
import { HeroImage } from "@/ui/images/HeroImage";

const validateSearchSchema = z.object({
  email: z.string().email(),
  expireAt: z.date(),
});

export const Route = createFileRoute("/register/$accountRegistrationId/")({
  component: CompleteAccountRegistrationPage,
  validateSearch: validateSearchSchema,
  errorComponent: ErrorPage,
});

function CompleteAccountRegistrationPage() {
  const { accountRegistrationId } = Route.useParams();
  const { email, expireAt } = Route.useSearch();

  return (
    <main className="flex min-h-screen flex-col">
      <div className="flex grow flex-col gap-4 md:flex-row">
        <div className="flex flex-col items-center justify-center gap-6 md:w-2/5 p-6">
          <CompleteAccountRegistrationForm
            email={email}
            expireAt={expireAt}
            accountRegistrationId={accountRegistrationId}
          />
        </div>
        <div className="flex items-center justify-center p-6 bg-gray-50 md:w-3/5 md:px-28 md:py-12">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
