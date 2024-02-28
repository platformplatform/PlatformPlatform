import { useLingui } from "@lingui/react";
import { z } from "zod";
import heroDesktop from "@/app/hero-desktop.png";
import heroMobile from "@/app/hero-mobile.png";
import { SignUpVerifyForm } from "@/ui/Auth/SignUpVerifyForm";
import { useSearchParams } from "@/lib/router/router";

interface SignUpVerifyPageProps {
  params: {
    registrationId: string,
  };
}

export default function SignUpVerifyPage({ params: { registrationId } }: Readonly<SignUpVerifyPageProps>) {
  const { i18n } = useLingui();
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
          <img
            src={heroMobile}
            width={560}
            height={620}
            className="block md:hidden"
            alt={i18n.t("Screenshots of the dashboard project showing mobile versions")}
          />
          <img
            src={heroDesktop}
            width={1000}
            height={760}
            className="hidden md:block"
            alt={i18n.t("Screenshots of the dashboard project showing desktop and mobile versions")}
          />
        </div>
      </div>
    </main>
  );
}
