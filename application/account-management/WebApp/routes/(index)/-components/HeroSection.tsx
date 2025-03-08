import logoMark from "@/shared/images/logo-mark.svg";
import { t } from "@lingui/core/macro";
import { LoginButton } from "@repo/infrastructure/auth/LoginButton";
import { SignUpButton } from "@repo/infrastructure/auth/SignUpButton";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { Link } from "@repo/ui/components/Link";
import { Popover } from "@repo/ui/components/Popover";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { ArrowRightIcon, ChevronDownIcon, GithubIcon } from "lucide-react";
import type React from "react";
import { DialogTrigger } from "react-aria-components";
import { heroDesktopUrl, heroMobileUrl, logoWrap } from "./cdnImages";

// HeroSection: A functional component that displays the hero section
export function HeroSection() {
  return (
    <div className="flex flex-col items-center bg-muted">
      <div className="flex w-fit flex-col-reverse items-center justify-between gap-4 px-2 pt-8 pb-24 md:w-full md:flex-row md:gap-2 xl:px-24">
        <div className="flex pt-4 md:pt-0">
          <img className="hidden lg:block" src={logoWrap} alt="logo" />
          <img className="h-20 md:h-12 lg:hidden" src={logoMark} alt="logo" />
        </div>
        <div className="flex flex-col items-center justify-start gap-2 sm:flex-row md:gap-4 lg:gap-8">
          <ProductLink />
          <ResourcesLink />
          <DocumentationLink />
        </div>
        <div className="flex w-full items-center justify-between gap-2 md:w-fit md:gap-4">
          <Link href="https://github.com/platformplatform/PlatformPlatform">
            <GithubIcon className="wmax-5 h-5" />
            <span className="md:hidden lg:inline">Github</span>
          </Link>
          <ThemeModeSelector aria-label={t`Toggle theme`} />
          <LoginButton variant="ghost">Log in</LoginButton>
          <SignUpButton variant="primary">Get started today</SignUpButton>
        </div>
      </div>
      <div className="flex flex-col items-center gap-4 px-8 py-12 text-center md:px-48">
        <FeatureTag />
        <ProductSubtitle />
        <ProductTitle />
        <ProductDescription />
        <ActionButtons />
      </div>
      <div className=" flex justify-center px-24">
        <img className="hidden rounded-t-2xl md:block" src={heroDesktopUrl} alt="Footer" />
        <img className="md:hidden" src={heroMobileUrl} alt="Footer" />
      </div>
    </div>
  );
}

// ProductLink: A functional component that displays the product link with dropdown
function ProductLink() {
  return (
    <TopLink title="Product">
      <Link href="/">Product</Link>
      <Link href="/">Product</Link>
      <Link href="/">Product</Link>
      <Link href="/">Product</Link>
    </TopLink>
  );
}

// ResourcesLink: A functional component that displays the resources link with dropdown
function ResourcesLink() {
  return (
    <TopLink title="Resources">
      <Link href="/">Product</Link>
      <Link href="/">Product</Link>
      <Link href="/">Product</Link>
      <Link href="/">Product</Link>
    </TopLink>
  );
}

// DocumentationLink: A functional component that displays the documentation link with dropdown
function DocumentationLink() {
  return (
    <TopLink title="Documentation">
      <Link href="/">Product</Link>
      <Link href="/">Product</Link>
      <Link href="/">Product</Link>
      <Link href="/">Product</Link>
    </TopLink>
  );
}

type TopLinkProps = {
  title: string;
  children: React.ReactNode;
};

function TopLink({ title, children }: Readonly<TopLinkProps>) {
  return (
    <DialogTrigger>
      <Button variant="ghost">
        {title} <ChevronDownIcon />
      </Button>
      <Popover>
        <Dialog className="flex flex-col">{children}</Dialog>
      </Popover>
    </DialogTrigger>
  );
}

function FeatureTag() {
  return (
    <Button variant="primary" className="gap-2 rounded-xl px-2">
      <Badge variant="secondary">New feature</Badge>
      PassKeys are here
      <ArrowRightIcon className="h-4 w-4" />
    </Button>
  );
}

function ProductTitle() {
  return (
    <h3 className="font-semibold text-4xl md:text-6xl">
      Launch your product in minutes - on the best Open Source platform
    </h3>
  );
}

function ProductSubtitle() {
  return <h4 className="font-semibold text-base text-muted-foreground ">Super. Simple. SaaS.</h4>;
}

function ProductDescription() {
  return (
    <p className="text-muted-foreground text-xl">
      Free, Open-Source .NET, React and Infrastructure kit for Startup and Enterprise.
    </p>
  );
}

function ActionButtons() {
  return (
    <div className="flex justify-center gap-4">
      <Button variant="outline">Book a demo</Button>
      <SignUpButton variant="primary">Get started today</SignUpButton>
    </div>
  );
}
