import { Button } from "@/ui/components/Button";

const createAccountUrl = "https://platformplatformgithub.blob.core.windows.net/create-account-s3.png";
const darkmodeUrl = "https://platformplatformgithub.blob.core.windows.net/darkmode-button.svg?url";

// FeatureSection3: A functional component that displays the third feature section
export function FeatureSection3() {
  return (
    <div className="flex flex-col gap-8 bg-gray-50 py-24 w-full">
      <div className="flex flex-col gap-8 md:px-32 pt-16 px-8 text-start w-full lg:w-2/3">
        <FeatureTitle />
      </div>
      <div className="flex flex-col lg:flex-row gap-4 justify-between items-center">
        <div className="flex flex-col gap-4 pl-8 md:pl-32 fill lg:w-1/2">
          <FeatureList />
        </div>
        <div className="flex lg:w-1/2 justify-end border-4 rounded-xl shadow-xl">
          <img
            src={createAccountUrl}
            alt="Mockup"
            className="rounded-lg"
          />
        </div>
      </div>
    </div>
  );
}

export default FeatureSection3;

// FeatureTitle: A functional component that displays the title of the feature section
function FeatureTitle() {
  return (
    <>
      <h1 className="text-amber-600 text-base font-semibold text-start">
        FEATURES
      </h1>
      <h2 className="text-gray-900 text-4xl font-semibold">
        Focus on building and launching your core product. We got your back -
        Enterprise grade
      </h2>
      <p className="text-slate-600 text-md md:text-xl font-normal">
        PlatformPlatform uses best practices and enterprise-grade tech to
        provide the foundation you need to build your core product.
      </p>
    </>
  );
}

// FeatureList: A functional component that displays a list of features
function FeatureList() {
  return (
    <>
      <div className="flex flex-col md:gap-8 gap-4">
        <h3 className="text-gray-900 text-2xl font-semibold">
          Sign up and Sign in - built right in
        </h3>
        <p className="text-slate-600 text-md md:text-xl font-normal">
          We’ve built user authentication and authorisation right in, so you
          don’t have to. With enterprise security grade and using best practice.
          <br />
          No need for passwords and the problems of password resets, but just
          seamless one click sign up and sign in using magic links and Passkeys.
          <br />
          And you control your brand of course.
        </p>
      </div>
      <div className="flex flex-col md:gap-8 gap-4">
        <h3 className="text-gray-900 text-2xl font-semibold">
          User Management and Multi-tenancy
        </h3>
        <p className="text-slate-600 text-md md:text-xl font-normal">
          Onboard and easily manage your users using permissions and roles and
          let them create organizations for your multi-tenant SaaS product.
        </p>
      </div>
      <div className="flex flex-col md:gap-8 gap-4">
        <h3 className="text-gray-900 text-2xl font-semibold">
          Dark Side of the Force
        </h3>
        <p className="text-slate-600 text-md md:text-xl font-normal">
          Besides being Enterprise grade, with a security score of 100% and ISO
          compliancy, everything is not always as bright as it seems.
        </p>
        <div className="flex items-center text-slate-600 text-md md:text-xl font-normal gap-4">
          <span>Just click the </span>
          <Button variant="neutral" className="flex p-2 items-center">
            <img
              src={darkmodeUrl}
              alt="Dark Mode Toggle"
              className="flex w-full h-full"
            />
          </Button>
          <span> button to switch to the Dark Side.</span>
        </div>
      </div>
    </>
  );
}
