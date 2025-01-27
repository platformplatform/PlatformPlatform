import { DialogTrigger } from "react-aria-components";
import { ArrowRightIcon, ChevronDownIcon, GithubIcon } from "lucide-react";
import { heroDesktopUrl, heroMobileUrl, logoWrap } from "./cdnImages";
import { Button } from "@repo/ui/components/Button";
import { Popover } from "@repo/ui/components/Popover";
import { Dialog } from "@repo/ui/components/Dialog";
import { Link } from "@repo/ui/components/Link";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { Badge } from "@repo/ui/components/Badge";
import logoMark from "@/shared/images/logo-mark.svg";
import { LoginButton } from "@repo/infrastructure/auth/LoginButton";
import { SignUpButton } from "@repo/infrastructure/auth/SignUpButton";
import { t } from "@lingui/core/macro";

// HeroSection: A functional component that displays the hero section
export function HeroSection() {
  return (
    <div className="flex flex-col bg-muted items-center">
      <div className="flex flex-col-reverse md:flex-row gap-4 md:gap-2 pt-8 pb-24 xl:px-24 px-2 items-center justify-between w-fit md:w-full">
        <div className="flex pt-4 md:pt-0">
          <img className="hidden lg:block" src={logoWrap} alt="logo" />
          <img className="lg:hidden h-20 md:h-12" src={logoMark} alt="logo" />
        </div>
        <div className="flex flex-col justify-start gap-2 md:gap-4 lg:gap-8 sm:flex-row items-center">
          <ProductLink />
          <ResourcesLink />
          <DocumentationLink />
        </div>
        <div className="flex w-full md:w-fit justify-between gap-2 md:gap-4 items-center">
          <Link href="https://github.com/platformplatform/PlatformPlatform">
            <GithubIcon className="wmax-5 h-5" />
            <span className="md:hidden lg:inline">Github</span>
          </Link>
          <ThemeModeSelector aria-label={t`Toggle theme`} />
          <LoginButton variant="ghost">Log in</LoginButton>
          <SignUpButton variant="primary">Get started today</SignUpButton>
        </div>
      </div>
      <div className="flex flex-col items-center gap-4 py-12 px-8 md:px-48 text-center">
        <FeatureTag />
        <ProductSubtitle />
        <ProductTitle />
        <ProductDescription />
        <ActionButtons />
      </div>
      <div className=" px-24 justify-center flex">
        <img className="hidden md:block rounded-t-2xl" src={heroDesktopUrl} alt="Footer" />
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
    <Button variant="primary" className="rounded-xl px-2 gap-2">
      <Badge variant="secondary">New feature</Badge>
      PassKeys are here
      <ArrowRightIcon className="h-4 w-4" />
    </Button>
  );
}

function ProductTitle() {
  return (
    <h3 className="text-4xl font-semibold md:text-6xl">
      Launch your product in minutes - on the best Open Source platform
    </h3>
  );
}

function ProductSubtitle() {
  return <h4 className="text-muted-foreground text-base font-semibold ">Super. Simple. SaaS.</h4>;
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
