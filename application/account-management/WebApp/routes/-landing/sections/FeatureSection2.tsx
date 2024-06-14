// FeatureSection2: Displays a section with features
export function FeatureSection2() {
  return (
    <div className="flex flex-col md:flex-row gap-8 bg-white py-24 px-8 md:px-32">
      <div className="flex flex-col md:w-1/3 grow gap-4 text-gray-900 text-4xl font-semibold text-start pr-8">
        <h1 className="text-amber-600 text-base font-semibold text-start">
          FEATURES
        </h1>
        All the features you need to build anything you want
      </div>
      <div className="flex md:flex-row flex-col gap-16 w-2/3">
        <div className="grid gap-x-16 gap-y-8 grid-cols-1 md:grid-cols-2 grid-rows-3 text-black md:max-w-full">
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

interface FeatureBlockProps { title: string, content: string, }

// FeatureBlock: Displays a single feature
function FeatureBlock({ title, content }: FeatureBlockProps) {
  return (
    <div className="flex flex-col gap-4 h-1/3">
      <div className="text-gray-900 text-xl font-semibold text-start">
        {title}
      </div>
      <div className="text-slate-600 text-base font-normal text-start">
        {content}
      </div>
    </div>
  );
}
