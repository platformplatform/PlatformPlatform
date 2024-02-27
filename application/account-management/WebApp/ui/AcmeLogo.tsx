import { PawPrintIcon } from "lucide-react";

export default function AcmeLogo() {
  return (
    <div
      className="flex flex-row gap-2 items-center leading-none text-white dark:black"
    >
      <PawPrintIcon className="h-12 w-12 rotate-[15deg]" />
      <p className="font-extrabold font-serif text-[44px]">acme</p>
    </div>
  );
}
