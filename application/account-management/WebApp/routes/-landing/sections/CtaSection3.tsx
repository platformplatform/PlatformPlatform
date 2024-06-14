import { Button } from "@/ui/components/Button";

// CtaSection3: A functional component that displays a call to action
export function CtaSection3() {
  return (
    <div className="flex flex-col gap-4 items-center text-center bg-white px-8 md:px-24 pt-12 pb-24">
      <div className="bg-gray-50 py-16 md:py-32 rounded-xl w-full">
        <div className="flex flex-col gap-8 text-center">
          <h2 className="text-gray-900 text-4xl md:text-6xl font-semibold">
            Start scaling your business today
          </h2>
          <p className="text-slate-600 text-xl font-normal">
            Join Startups and Enterprises already building on PlatformPlatform
          </p>
          <div className="flex flex-col md:gap-8 items-center">
            {/* Button component is used to display a call to action */}
            <Button variant="neutral" className="text-nowrap">
              Get started for free
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
