import { Button } from "@repo/ui/components/Button";
import { TextField } from "@repo/ui/components/TextField";
import { githubLogo, linkedinLogo, logoWrap, slackLogo, twitterLogo, youtubeLogo } from "./cdnImages";

// FooterSection: A functional component that displays the footer section
export function FooterSection() {
  return (
    <>
      <div className="flex w-full items-center gap-8 bg-muted px-16 py-16 md:px-24">
        <div className="flex flex-grow flex-col gap-4">
          <div className="font-semibold text-foreground text-xl">Join our newsletter</div>
          <div className="font-normal text-base text-muted-foreground">Technology that has your back.</div>
        </div>
        <div className="right flex flex-col items-center gap-4 md:flex-row">
          <TextField type="email" placeholder="Enter your email" />
          {/* Button component is used to display a call to action */}
          <Button variant="primary">Subscribe</Button>
        </div>
      </div>
      <div className="flex w-full flex-col bg-background px-24 py-16">
        <div className="flex max-w-sm flex-grow flex-row items-center justify-between">
          <div className="flex flex-col gap-8">
            <img src={logoWrap} alt="Logo Wrap" loading="lazy" />
            <div className="font-normal text-base text-muted-foreground">
              Build amazing products on enterprise grade and open source technology.
            </div>
          </div>
        </div>
        <hr className="my-8 border-gray-300 border-t" />
        <div className="flex flex-col items-center justify-between md:flex-row">
          <div className="mb-4 font-normal text-base text-muted-foreground md:mb-0">
            © 2024 PlatformPlatform. All rights reserved.
          </div>
          <div className="flex flex-row gap-8">
            <img src={twitterLogo} alt="Twitter Logo" loading="lazy" />
            <img src={linkedinLogo} alt="LinkedIn Logo" loading="lazy" />
            <img src={youtubeLogo} alt="YouTube Logo" loading="lazy" />
            <img src={githubLogo} alt="GitHub Logo" loading="lazy" />
            <img src={slackLogo} alt="Slack Logo" loading="lazy" />
          </div>
        </div>
      </div>
    </>
  );
}
