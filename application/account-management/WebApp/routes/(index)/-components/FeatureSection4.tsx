// FeatureSection4: A functional component that displays a section with features
export function FeatureSection4() {
  return (
    <div className="flex flex-col md:flex-row gap-16 bg-muted py-24 md:px-24 px-8">
      <div className="flex flex-col md:w-1/4 grow gap-4 text-foreground text-4xl font-semibold text-start">
        <h1 className="text-amber-600 text-base font-semibold text-start">FEATURES</h1>
        Built by Founders, Engineers and Designers
        <div className="text-muted-foreground text-xl font-normal">
          Skip the many months it takes to build an enterprise grade and production ready setup. We’ve done the hard
          work for you so you can focus on your core product.
        </div>
      </div>
      <div className="flex md:flex-row flex-col gap-16 w-3/4">
        <div className="grid gap-x-16 gap-y-8 grid-cols-1 md:grid-cols-2 grid-rows-3 text-black md:max-w-full">
          {/* FeatureBlock components are used to display individual features */}
          <FeatureBlock
            title="Crafted for Startups"
            content="Boost your “Time to first feature” with a full scale enterprise grade production environment."
          />
          <FeatureBlock
            title="Production ready"
            content="We strive to make things hardened and actual production ready. We put time and effort into crafting the best possible startup kit."
          />
          <FeatureBlock
            title="Open Source"
            content="The best way to share our experience from decades in the tech areana is to build in the open."
          />
          <FeatureBlock
            title="Enterprise grade"
            content="Multi tenancy to fit any organisation structure in B2B and B2C businesses including audit logs."
          />
          <FeatureBlock
            title="Production ready"
            content="We strive to make things hardened and actual production ready. We put time and effort into crafting the best possible startup kit."
          />
          <FeatureBlock
            title="Boring tech"
            content="We use the latest version of “boring” technologies proven through time. We have made a ton of decisions so that you can start building on a trustworthy tech stack."
          />
        </div>
      </div>
    </div>
  );
}

// FeatureBlock: A functional component that displays a single feature
function FeatureBlock({ title, content }: Readonly<FeatureBlockProps>) {
  return (
    <div className="flex flex-col gap-4 h-1/3">
      <div className="text-foreground text-xl font-semibold text-start">{title}</div>
      <div className="text-muted-foreground text-base font-normal text-start">{content}</div>
    </div>
  );
}

// FeatureBlockProps: Type definition for the props of FeatureBlock component
interface FeatureBlockProps {
  title: string;
  content: string;
}
