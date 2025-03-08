import { frame1, frame2, frame3, frame4, frame5, frame6 } from "./cdnImages";

// TechnologySection: A functional component that displays the technology section
export function TechnologySection() {
  return (
    <div className="flex flex-col gap-16 bg-background px-8 py-24 text-center md:px-32">
      <div className="flex w-2/3 flex-col gap-4 text-start font-semibold text-3xl text-foreground">
        <h1 className="text-start font-semibold text-amber-600 text-base">FEATURES</h1>
        <p className="text-start font-semibold text-4xl text-foreground">
          Standing on the shoulders of giants. Building blocks of PlatformPlatform
        </p>
      </div>
      <div className="flex flex-col gap-16 md:flex-row">
        <div className="grid grid-cols-1 grid-rows-2 gap-x-16 gap-y-8 text-black md:grid-cols-3">
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
function FeatureBlock({ title, content, image }: Readonly<FeatureBlockProps>) {
  return (
    <div className="flex h-1/3 flex-col gap-4">
      <div>
        <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-white p-0 shadow dark:shadow-gray-600">
          <img src={image} alt={title} loading="lazy" />
        </div>
      </div>
      <div className="text-start font-semibold text-foreground text-xl">{title}</div>
      <div className="text-start font-normal text-base text-muted-foreground">{content}</div>
    </div>
  );
}
