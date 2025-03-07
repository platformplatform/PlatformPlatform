import {
  accountSettingsUrl,
  createAccountUrl,
  darkModeUrl,
  editProfileUrl,
  enterCodeUrl,
  lightModeUrl,
  profileEditDarkUrl,
  profileEditLightUrl,
  usersUrl
} from "./cdnImages";

// FeatureSection: A functional component that displays the feature section
export function FeatureSection() {
  return (
    <div className="flex flex-col gap-8 bg-background pb-12 text-center">
      <div className="flex flex-col gap-8 self-center px-8 pt-24 md:w-1/2">
        <h1 className="font-semibold text-amber-600 text-base">FEATURES</h1>
        <h2 className="font-semibold text-2xl text-foreground md:text-6xl">
          All-in-one Infrastructure for any business
        </h2>
        <p className="font-normal text-md text-muted-foreground md:text-xl">
          Get a full enterprise grade production environment running within 1 hour. From development, infrastructure as
          code to fully deployed production application with a security score of 100%.
        </p>
      </div>
      <div className="flex flex-row justify-center gap-4 px-16 md:pr-0">
        <div className="flex flex-col gap-4">
          <img
            className="rounded-lg shadow-gray-400 shadow-xl dark:shadow-gray-800"
            src={createAccountUrl}
            alt="Create Account"
            loading="lazy"
          />
          <img
            className="rounded-lg shadow-gray-400 shadow-xl dark:shadow-gray-800"
            src={enterCodeUrl}
            alt="Enter Code"
            loading="lazy"
          />
        </div>
        <div className="flex flex-col gap-4">
          <div className="flex flex-row gap-2">
            <img
              className="rounded-lg shadow-gray-400 shadow-xl dark:shadow-gray-800"
              src={profileEditDarkUrl}
              alt="Profile Edit Dark"
              loading="lazy"
            />
            <div>
              <img
                className="rounded-lg shadow-gray-400 shadow-xl dark:shadow-gray-800"
                src={darkModeUrl}
                alt="Dark Mode"
                loading="lazy"
              />
            </div>
            <div>
              <img
                className="rounded-lg shadow-gray-400 shadow-xl dark:shadow-gray-800"
                src={lightModeUrl}
                alt="Light Mode"
                loading="lazy"
              />
            </div>
          </div>
          <img
            className="rounded-lg shadow-gray-400 shadow-xl dark:shadow-gray-800"
            src={editProfileUrl}
            alt="Edit Profile"
            loading="lazy"
          />
        </div>
        <div className="hidden flex-col gap-4 md:flex">
          <img
            className="rounded-lg shadow-gray-400 shadow-xl dark:shadow-gray-800"
            src={accountSettingsUrl}
            alt="Account Settings"
            loading="lazy"
          />
          <img
            className="rounded-lg shadow-gray-400 shadow-xl dark:shadow-gray-800"
            src={profileEditLightUrl}
            alt="Edit Profile Light"
            loading="lazy"
          />
        </div>
        <div className="hidden flex-col gap-4 md:flex">
          <img
            className="rounded-lg shadow-gray-400 shadow-xl dark:shadow-gray-800"
            src={usersUrl}
            alt="Users"
            loading="lazy"
          />
        </div>
      </div>
    </div>
  );
}
