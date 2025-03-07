import { SignUpButton } from "@repo/infrastructure/auth/SignUpButton";

// CtaSection3: A functional component that displays a call to action
export function CtaSection3() {
  return (
    <div className="flex flex-col items-center gap-4 bg-background px-8 pt-12 pb-24 text-center md:px-24">
      <div className="w-full rounded-xl bg-muted py-16 md:py-32">
        <div className="flex flex-col gap-8 text-center">
          <h2 className="font-semibold text-4xl text-foreground md:text-6xl">Start scaling your business today</h2>
          <p className="font-normal text-muted-foreground text-xl">
            Join Startups and Enterprises already building on PlatformPlatform
          </p>
          <div className="flex flex-col items-center md:gap-8">
            {/* Button component is used to display a call to action */}
            <SignUpButton variant="primary">Get started for free</SignUpButton>
          </div>
        </div>
      </div>
    </div>
  );
}
