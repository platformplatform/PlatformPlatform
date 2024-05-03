import { Button } from "@/ui/components/Button";

const logoWrap = "https://platformplatformgithub.blob.core.windows.net/logo-wrap.svg?url";
const twitterLogo = "https://platformplatformgithub.blob.core.windows.net/twitter-x.svg?url";
const linkedinLogo = "https://platformplatformgithub.blob.core.windows.net/linkedin.svg?url";
const youtubeLogo = "https://platformplatformgithub.blob.core.windows.net/youtube-logo.svg?url";
const githubLogo = "https://platformplatformgithub.blob.core.windows.net/github2.svg?url";
const slackLogo = "https://platformplatformgithub.blob.core.windows.net/slack2.svg?url";

// FooterSection: A functional component that displays the footer section
export function FooterSection() {
  return (
    <>
      <div className="w-full bg-gray-50 flex items-center gap-8 py-16 px-16 md:px-24">
        <div className="flex-grow flex flex-col gap-4">
          <div className="text-gray-900 text-xl font-semibold">
            Join our newsletter
          </div>
          <div className="text-slate-600 text-base font-normal">
            technology that has your back.
          </div>
        </div>
        <div className="flex items-center gap-4 md:flex-row flex-col right">
          <input
            type="email"
            placeholder="Enter your email"
            className="bg-white rounded-lg shadow border border-gray-300 text-gray-500 text-base font-normal p-1.5"
          />
          {/* Button component is used to display a call to action */}
          <Button variant="neutral">Subscribe</Button>
        </div>
      </div>
      <div className="w-full px-24 bg-white flex flex-col py-16">
        <div className="flex flex-row items-center flex-grow justify-between max-w-sm">
          <div className="flex gap-8 flex-col">
            <img src={logoWrap} alt="Logo Wrap" />
            <div className="text-slate-600 text-base font-normal">
              Build amazing products on enterprise grade and open source
              technology.
            </div>
          </div>
        </div>
        <hr className="border-t border-gray-300 my-8" />
        <div className="flex flex-col md:flex-row justify-between items-center">
          <div className="text-slate-600 text-base font-normal mb-4 md:mb-0">
            © 2024 PlatformPlatform. All rights reserved.
          </div>
          <div className="flex flex-row gap-8">
            <img src={twitterLogo} alt="Twitter Logo" />
            <img src={linkedinLogo} alt="LinkedIn Logo" />
            <img src={youtubeLogo} alt="YouTube Logo" />
            <img src={githubLogo} alt="GitHub Logo" />
            <img src={slackLogo} alt="Slack Logo" />
          </div>
        </div>
      </div>
    </>
  );
}
