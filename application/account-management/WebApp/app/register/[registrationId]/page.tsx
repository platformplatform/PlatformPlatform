import { z } from "zod";
import { SignUpVerifyForm } from "@/ui/Auth/SignUpVerifyForm";
import { useSearchParams } from "@/lib/router/router";
import { HeroImage } from "@/ui/images/HeroImage";

interface SignUpVerifyPageProps {
  params: {
    registrationId: string,
  };
}

export default function SignUpVerifyPage({ params: { registrationId } }: Readonly<SignUpVerifyPageProps>) {
  const [searchParams] = useSearchParams();

  const email = z.string().email().parse(searchParams.get("email"));
  const expireAt = z.date().parse(new Date(Number.parseInt(searchParams.get("expireAt") ?? "", 10)));

  return (
    <main className="flex min-h-screen flex-col">
      <div className="flex grow flex-col gap-4 md:flex-row">

        <div className="flex flex-col items-center justify-center gap-6 md:w-2/5 p-6">
          <SignUpVerifyForm email={email} expireAt={expireAt} registrationId={registrationId} />
        </div>
        <div className="flex items-center justify-center p-6 bg-gray-50 md:w-3/5 md:px-28 md:py-12">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
