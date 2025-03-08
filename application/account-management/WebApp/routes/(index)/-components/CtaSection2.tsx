import { Button } from "@repo/ui/components/Button";
import { calenderMockupUrl } from "./cdnImages";

// CtaSection2: A functional component that displays the second call to action section
export function CtaSection2() {
  return (
    <div className="flex justify-center bg-background px-8 pt-24 pb-12 shadow-lg md:px-24">
      <div className="flex flex-col items-center justify-between gap-16 rounded-3xl bg-gray-800 p-4 pt-16 md:flex-row md:px-8 md:py-16">
        <div className="flex w-1/3 flex-col items-center gap-8 px-8 md:items-start">
          <div className="font-semibold text-4xl text-white">
            Get lightyears ahead and get your product in the hands of your customers
          </div>
          <div className="font-normal text-slate-200 text-xl">No credit cards or hidden fees. Just Open Source.</div>
          <Button variant="secondary">Get started today - it’s free</Button>
        </div>
        <div className="w-min-6xl">
          <img src={calenderMockupUrl} alt="Create Account" loading="lazy" />
        </div>
      </div>
    </div>
  );
}
