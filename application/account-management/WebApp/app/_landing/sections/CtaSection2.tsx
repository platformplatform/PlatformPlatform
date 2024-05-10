import { Button } from "@/ui/components/Button";

const calenderMockupUrl = "https://platformplatformgithub.blob.core.windows.net/mockup-calender.png";

// CtaSection2: A functional component that displays the second call to action section
export function CtaSection2() {
  return (
    <div className="flex bg-white justify-center pb-12 pt-24 px-8 md:px-24 shadow-lg">
      <div className="flex bg-gray-800 justify-between items-center rounded-3xl pt-16 p-4 md:py-16 md:px-8 gap-16 md:flex-row flex-col">
        <div className="flex flex-col items-center md:items-start gap-8 px-8 w-1/3">
          <div className="text-white text-4xl font-semibold">
            Get lightyears ahead and get your product in the hands of your customers
          </div>
          <div className="text-slate-200 text-xl font-normal">
            No credit cards or hidden fees. Just Open Source.
          </div>
          <Button className="bg-gray-600 rounded-lg shadow whitespace-nowrap">
            Get started today - it’s free
          </Button>
        </div>
        <div className="w-min-6xl">
          <img
            src={calenderMockupUrl}
            alt="Create Account"
          />
        </div>
      </div>
    </div>
  );
}
