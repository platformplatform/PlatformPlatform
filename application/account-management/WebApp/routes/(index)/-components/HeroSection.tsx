import { DialogTrigger } from "react-aria-components";
import { ArrowRightIcon, ChevronDownIcon, GithubIcon } from "lucide-react";
import { useNavigate } from "@tanstack/react-router";
import { Button } from "@repo/ui/components/Button";
import { Popover } from "@repo/ui/components/Popover";
import { Dialog } from "@repo/ui/components/Dialog";
import { Link } from "@repo/ui/components/Link";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { Badge } from "@repo/ui/components/Badge";

const logoWrap = "https://platformplatformgithub.blob.core.windows.net/logo-wrap.svg?url";
const heroimgDesktop = "https://platformplatformgithub.blob.core.windows.net/hero-image-desktop.png";
const heroimgMobile = "https://platformplatformgithub.blob.core.windows.net/hero-image-mobile.webp";

// HeroSection: A functional component that displays the hero section
export function HeroSection() {
  const navigate = useNavigate();
  return (
    <div className="flex flex-col bg-muted">
      <div className="flex flex-col gap-2 md:flex-row justify-between pt-8 pb-24 xl:px-24 px-2">
        <div className="flex flex-col grow justify-start gap-2 md:gap-4 lg:gap-8 lg:justify-start md:flex-row items-center ">
          <img src={logoWrap} alt="author" />
          <ProductLink />
          <ResourcesLink />
          <DocumentationLink />
          <Link href="https://github.com/platformplatform/PlatformPlatform">
            <GithubIcon className="wmax-5 h-5" />
            <span className="md:hidden lg:inline">Github</span>
          </Link>
          <ThemeModeSelector />
        </div>
        <div className="flex flex-col md:gap-4 md:flex-row items-center">
          <Button onPress={() => navigate({ to: "/login" })} variant="ghost">
            Log in
          </Button>
          <Button onPress={() => navigate({ to: "/register" })} variant="primary">
            Sign up
          </Button>
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
        <img className="hidden md:block" src={heroimgDesktop} alt="Footer" />
        <img className="md:hidden" src={heroimgMobile} alt="Footer" />
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
  const navigate = useNavigate();
  return (
    <div className="flex justify-center gap-4">
      <Button variant="outline">Book a demo</Button>
      <Button onPress={() => navigate({ to: "/register" })} variant="primary">
        Get started today
      </Button>
    </div>
  );
}
