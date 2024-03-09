import { z } from "zod";
import { CompleteAccountRegistrationForm } from "../components/CompleteAccountRegistrationForm.tsx";
import { useSearchParams } from "@/lib/router/router";
import { HeroImage } from "@/ui/images/HeroImage";

interface CompleteAccountRegistrationPageProps {
  params: {
    accountRegistrationId: string,
  };
}

export default function CompleteAccountRegistrationPage({
  params: { accountRegistrationId },
}: Readonly<CompleteAccountRegistrationPageProps>) {
  const [searchParams] = useSearchParams();

  const email = z.string().email().parse(searchParams.get("email"));
  const expireAt = z.date().parse(new Date(Number.parseInt(searchParams.get("expireAt") ?? "", 10)));

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
