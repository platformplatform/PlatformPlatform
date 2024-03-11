import { StartAccountRegistrationForm } from "./components/StartAccountRegistrationForm.tsx";
import { HeroImage } from "@/ui/images/HeroImage";

export default function AccountRegistrationPage() {
  return (
    <main className="flex min-h-screen flex-col">
      <div className="flex grow flex-col gap-4 md:flex-row">

        <div className="flex flex-col items-center justify-center gap-6 md:w-2/5 p-6">
          <StartAccountRegistrationForm />
        </div>
        <div className="flex items-center justify-center p-6 bg-gray-50 md:w-3/5 md:px-28 md:py-12">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
