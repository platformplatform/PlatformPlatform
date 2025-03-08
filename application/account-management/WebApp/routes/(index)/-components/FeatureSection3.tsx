import { t } from "@lingui/core/macro";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { createAccountUrl } from "./cdnImages";

// FeatureSection3: A functional component that displays the third feature section
export function FeatureSection3() {
  return (
    <div className="flex w-full flex-col gap-8 bg-muted py-24">
      <div className="flex w-full flex-col gap-8 px-8 pt-16 text-start md:px-32 lg:w-2/3">
        <FeatureTitle />
      </div>
      <div className="flex flex-col items-center justify-between gap-4 lg:flex-row">
        <div className="fill flex flex-col gap-4 pl-8 md:pl-32 lg:w-1/2">
          <FeatureList />
        </div>
        <div className="flex justify-end rounded-xl border-4 shadow-xl lg:w-1/2">
          <img src={createAccountUrl} alt="Mockup" className="rounded-lg" loading="lazy" />
        </div>
      </div>
    </div>
  );
}

// FeatureTitle: A functional component that displays the title of the feature section
function FeatureTitle() {
  return (
    <>
      <h1 className="text-start font-semibold text-amber-600 text-base">FEATURES</h1>
      <h2 className="font-semibold text-4xl text-foreground">
        Focus on building and launching your core product. We got your back - Enterprise grade
      </h2>
      <p className="font-normal text-md text-muted-foreground md:text-xl">
        PlatformPlatform uses best practices and enterprise-grade tech to provide the foundation you need to build your
        core product.
      </p>
    </>
  );
}

// FeatureList: A functional component that displays a list of features
function FeatureList() {
  return (
    <>
      <div className="flex flex-col gap-4 md:gap-8">
        <h3 className="font-semibold text-2xl text-foreground">Sign up and Log in - built right in</h3>
        <p className="font-normal text-md text-muted-foreground md:text-xl">
          We’ve built user authentication and authorisation right in, so you don’t have to. With enterprise security
          grade and using best practice.
          <br />
          No need for passwords and the problems of password resets, but just seamless one click sign up and log in
          using magic links and Passkeys.
          <br />
          And you control your brand of course.
        </p>
      </div>
      <div className="flex flex-col gap-4 md:gap-8">
        <h3 className="font-semibold text-2xl text-foreground">User Management and Multi-tenancy</h3>
        <p className="font-normal text-md text-muted-foreground md:text-xl">
          Onboard and easily manage your users using permissions and roles and let them create organizations for your
          multi-tenant SaaS product.
        </p>
      </div>
      <div className="flex flex-col gap-4 md:gap-8">
        <h3 className="font-semibold text-2xl text-foreground">Dark Side of the Force</h3>
        <p className="font-normal text-md text-muted-foreground md:text-xl">
          Besides being Enterprise grade, with a security score of 100% and ISO compliancy, everything is not always as
          bright as it seems.
        </p>
        <div className="flex items-center gap-4 font-normal text-md text-muted-foreground md:text-xl">
          <span>Just click the </span>
          <ThemeModeSelector aria-label={t`Toggle theme`} />
          <span> button to switch to the Dark Side.</span>
        </div>
      </div>
    </>
  );
}
