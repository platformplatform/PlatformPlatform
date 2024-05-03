const frame1 = "https://platformplatformgithub.blob.core.windows.net/dotnet.svg?url";
const frame2 = "https://platformplatformgithub.blob.core.windows.net/react.svg?url";
const frame3 = "https://platformplatformgithub.blob.core.windows.net/github.svg?url";
const frame4 = "https://platformplatformgithub.blob.core.windows.net/azure.svg?url";
const frame5 = "https://platformplatformgithub.blob.core.windows.net/help-square.svg?url";
const frame6 = "https://platformplatformgithub.blob.core.windows.net/shield-star.svg?url";

// TechnologySection: A functional component that displays the technology section
export function TechnologySection() {
  return (
    <div className="flex flex-col gap-16 text-center bg-white py-24 px-8 md:px-32">
      <div className="flex flex-col gap-4 text-gray-900 text-3xl font-semibold text-start w-2/3">
        <h1 className="text-amber-600 text-base font-semibold text-start">
          FEATURES
        </h1>
        <p className="text-gray-900 text-4xl font-semibold text-start">
          Standing on the shoulders of giants. Building blocks of
          PlatformPlatform
        </p>
      </div>
      <div className="flex md:flex-row flex-col gap-16">
        <div className="grid gap-x-16 gap-y-8 grid-cols-1 md:grid-cols-3 grid-rows-2 text-black">
          {/* FeatureBlock component is used to display individual feature blocks */}
          <FeatureBlock
            title="Backend"
            content=".NET adhering to the principles of Clean Architecture, DDD, CQRS, and clean code"
            image={frame1}
          />
          <FeatureBlock
            title="Frontend"
            content="React using TypeScript, with a sleek fully localized UI and a mature accessible design system"
            image={frame2}
          />
          <FeatureBlock
            title="CI/CD"
            content="GitHub actions for fast passwordless deployments of application (Docker) and infrastructure (Bicep)"
            image={frame3}
          />
          <FeatureBlock
            title="Infrastructure"
            content="Cost efficient and scalable Azure PaaS services like Azure Container Apps, Azure SQL, etc."
            image={frame4}
          />
          <FeatureBlock
            title="Developer CLI"
            content="Extendable .NET CLI for DevEx - set up CI/CD is one command and a couple of questions"
            image={frame5}
          />
          <FeatureBlock
            title="100% Security Score"
            content="Azure standards security score of 100% - The most secure platform out there we dare say."
            image={frame6}
          />
        </div>
      </div>
    </div>
  );
}

interface FeatureBlockProps {
  title: string;
  content: string;
  image: string;
}

// FeatureBlock: A functional component that displays individual feature blocks
function FeatureBlock({ title, content, image }: FeatureBlockProps) {
  return (
    <div className="flex flex-col gap-4 h-1/3">
      <div>
        <div className="flex shadow rounded-lg w-12 h-12 justify-center p-0 items-center">
          <img src={image} alt={title} />
        </div>
      </div>
      <div className="text-gray-900 text-xl font-semibold text-start">
        {title}
      </div>
      <div className="text-slate-600 text-base font-normal text-start">
        {content}
      </div>
    </div>
  );
}
