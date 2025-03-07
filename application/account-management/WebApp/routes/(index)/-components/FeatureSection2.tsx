// FeatureSection2: Displays a section with features
export function FeatureSection2() {
  return (
    <div className="flex flex-col gap-8 bg-background px-8 py-24 md:flex-row md:px-32">
      <div className="flex grow flex-col gap-4 pr-8 text-start font-semibold text-4xl text-foreground md:w-1/3">
        <h1 className="text-start font-semibold text-amber-600 text-base">FEATURES</h1>
        All the features you need to build anything you want
      </div>
      <div className="flex w-2/3 flex-col gap-16 md:flex-row">
        <div className="grid grid-cols-1 grid-rows-3 gap-x-16 gap-y-8 text-black md:max-w-full md:grid-cols-2">
          <FeatureBlock
            title="Authentication and authorisation"
            content="Authentication and authorisation of your users are built right in using best practice and most secure industry standards."
          />
          <FeatureBlock
            title="User- & Account Management"
            content="Full user management with user roles and permissions using SCIM technology. Add, delete and edit users in a breeze."
          />
          <FeatureBlock
            title="Feature flags"
            content="We’ve built feature flags right in, so you can fully control the features you build yourself."
          />
          <FeatureBlock
            title="Scalable architecture"
            content=" Scale in terms of performance and organization without friction"
          />
          <FeatureBlock
            title="Billing and Subscription"
            content="Manage your billing and subscription of your product out of the box. We’ve prepared everything for you."
          />
          <FeatureBlock
            title="Accessibility built in"
            content="Build UI with accessibility in mind - not as an after thought"
          />
        </div>
      </div>
    </div>
  );
}

interface FeatureBlockProps {
  title: string;
  content: string;
}

// FeatureBlock: Displays a single feature
function FeatureBlock({ title, content }: Readonly<FeatureBlockProps>) {
  return (
    <div className="flex h-1/3 flex-col gap-4">
      <div className="text-start font-semibold text-foreground text-xl">{title}</div>
      <div className="text-start font-normal text-base text-muted-foreground">{content}</div>
    </div>
  );
}
