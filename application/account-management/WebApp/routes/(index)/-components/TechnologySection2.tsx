const infrastructure = "https://platformplatformgithub.blob.core.windows.net/infrastructure.png";

// TechnologySection2: A functional component that displays the technology section
export function TechnologySection2() {
  return (
    <div className="flex flex-col gap-8 bg-muted py-24 w-full px-8 md:px-24">
      <div className="flex flex-col gap-8 pt-16 text-start w-full lg:w-2/3">
        <div>
          {/* Display the section title */}
          <FeatureTitle />
        </div>
      </div>
      <div className="flex flex-col lg:flex-row-reverse gap-8 justify-between items-center">
        <div className="flex flex-col gap-4 pr-8 md:pr-24 fill lg:w-1/2">
          {/* Display the feature list */}
          <FeatureList />
        </div>
        <div className="flex lg:w-1/2 justify-start">
          <div className="rounded-xl shadow-xl h-fit">
            <img src={infrastructure} alt="Mockup" className="rounded-lg" loading="lazy" />
          </div>
        </div>
      </div>
    </div>
  );
}

export default TechnologySection2;

// FeatureTitle: A functional component that displays the section title
function FeatureTitle() {
  return (
    <div className="flex gap-4 flex-col">
      <h1 className="text-amber-600 text-base font-semibold text-start">TECHNOLOGY</h1>
      <h2 className="text-foreground text-4xl font-semibold">
        We’ve taken care of all the tech stuff, so you can focus on getting your product launched
      </h2>
      <p className="text-muted-foreground text-md md:text-xl font-normal">
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
        <h3 className="text-foreground text-2xl font-semibold">Monorepo containing all application code</h3>
        <p className="text-muted-foreground text-md md:text-xl font-normal">
          PlatformPlatform is a monorepo containing all application code, infrastructure, tools, libraries,
          documentation, etc. A monorepo is a powerful way to organize a codebase, used by Google, Facebook, Uber,
          Microsoft, etc.
        </p>
      </div>
      <div className="flex flex-col gap-4">
        <h3 className="text-foreground text-2xl font-semibold">Deploy Azure Infrastructure</h3>
        <p className="text-muted-foreground text-md md:text-xl font-normal">
          This is how it looks when GitHub workflows has deployed Azure Infrastructure:
        </p>
      </div>
      <div className="flex flex-col gap-4">
        <h3 className="text-foreground text-2xl font-semibold">100% Security score</h3>
        <p className="text-muted-foreground text-md md:text-xl font-normal">
          This is the security score after deploying PlatformPlatform resources to Azure. Achieving a 100% security
          score in Azure Defender for Cloud without exemptions is not trivial.
        </p>
      </div>
    </div>
  );
}
