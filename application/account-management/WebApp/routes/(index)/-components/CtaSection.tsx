import { Button } from "@repo/ui/components/Button";

// CtaSection: A functional component that displays a call to action
export function CtaSection() {
  return (
    <div className="flex flex-col gap-4 bg-muted px-8 py-24 text-center md:px-48">
      <div className="flex flex-col gap-8 px-8 text-center">
        <h2 className="font-semibold text-4xl text-foreground">A single solution for you to build on</h2>
        <p className="font-normal text-muted-foreground text-xl">
          Join Startups and Enterprises already building on PlatformPlatform
        </p>
        <div className="flex flex-col items-center md:gap-8">
          <Button variant="secondary">Get started today - it&apos;s free</Button>
        </div>
      </div>
    </div>
  );
}
