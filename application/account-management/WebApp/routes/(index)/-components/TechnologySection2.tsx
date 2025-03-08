import { infrastructure } from "./cdnImages";

// TechnologySection2: A functional component that displays the technology section
export function TechnologySection2() {
  return (
    <div className="flex w-full flex-col gap-8 bg-muted px-8 py-24 md:px-24">
      <div className="flex w-full flex-col gap-8 pt-16 text-start lg:w-2/3">
        <div>
          {/* Display the section title */}
          <FeatureTitle />
        </div>
      </div>
      <div className="flex flex-col items-center justify-between gap-8 lg:flex-row-reverse">
        <div className="fill flex flex-col gap-4 pr-8 md:pr-24 lg:w-1/2">
          {/* Display the feature list */}
          <FeatureList />
        </div>
        <div className="flex justify-start lg:w-1/2">
          <div className="h-fit rounded-xl shadow-xl">
            <img src={infrastructure} alt="Mockup" className="rounded-lg" loading="lazy" />
          </div>
        </div>
      </div>
    </div>
  );
}

// FeatureTitle: A functional component that displays the section title
function FeatureTitle() {
  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-start font-semibold text-amber-600 text-base">TECHNOLOGY</h1>
      <h2 className="font-semibold text-4xl text-foreground">
        We’ve taken care of all the tech stuff, so you can focus on getting your product launched
      </h2>
      <p className="font-normal text-md text-muted-foreground md:text-xl">
        Drawing on our expertise building true enterprise-grade products with millions of daily users in highly
        regulated sectors like healthcare, finance, government, etc., we help you create secure production-ready
        products.
      </p>
    </div>
  );
}

// FeatureList: A functional component that displays the feature list
function FeatureList() {
  return (
    <div className="flex flex-col gap-4 md:gap-16">
      <div className="flex flex-col gap-4">
        <h3 className="font-semibold text-2xl text-foreground">Monorepo containing all application code</h3>
        <p className="font-normal text-md text-muted-foreground md:text-xl">
          PlatformPlatform is a monorepo containing all application code, infrastructure, tools, libraries,
          documentation, etc. A monorepo is a powerful way to organize a codebase, used by Google, Facebook, Uber,
          Microsoft, etc.
        </p>
      </div>
      <div className="flex flex-col gap-4">
        <h3 className="font-semibold text-2xl text-foreground">Deploy Azure Infrastructure</h3>
        <p className="font-normal text-md text-muted-foreground md:text-xl">
          This is how it looks when GitHub workflows has deployed Azure Infrastructure:
        </p>
      </div>
      <div className="flex flex-col gap-4">
        <h3 className="font-semibold text-2xl text-foreground">100% Security score</h3>
        <p className="font-normal text-md text-muted-foreground md:text-xl">
          This is the security score after deploying PlatformPlatform resources to Azure. Achieving a 100% security
          score in Azure Defender for Cloud without exemptions is not trivial.
        </p>
      </div>
    </div>
  );
}
