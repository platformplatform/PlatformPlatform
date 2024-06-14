import { DialogTrigger } from "react-aria-components";
import { ArrowRightIcon, ChevronDownIcon, GithubIcon } from "lucide-react";
import { useNavigate } from "@tanstack/react-router";
import { Button } from "@/ui/components/Button";
import { Popover } from "@/ui/components/Popover";
import { Dialog } from "@/ui/components/Dialog";
import { Link } from "@/ui/components/Link";

const logoWrap = "https://platformplatformgithub.blob.core.windows.net/logo-wrap.svg?url";
const heroimgDesktop = "https://platformplatformgithub.blob.core.windows.net/hero-image-desktop.png";
const heroimgMobile = "https://platformplatformgithub.blob.core.windows.net/hero-image-mobile.webp";

// HeroSection: A functional component that displays the hero section
export function HeroSection() {
  const navigate = useNavigate();
  return (
    <div className="flex flex-col bg-gray-900">
      <div className="flex flex-col gap-2 md:flex-row justify-between pt-8 pb-24 xl:px-24 px-2">
        <div className="flex flex-col grow justify-start gap-2 md:gap-4 lg:gap-8 lg:justify-start md:flex-row items-center ">
          <img src={logoWrap} alt="author" />
          <ProductLink />
          <ResourcesLink />
          <DocumentationLink />
          <Link
            className="flex gap-1 items-center decoration-transparent"
            href="https://github.com/platformplatform/PlatformPlatform"
          >
            <GithubIcon className="wmax-5 h-5" />
            <span className="md:hidden lg:inline">Github</span>
          </Link>
        </div>
        <div className="flex flex-col md:gap-8 md:flex-row items-center">
          <Button onPress={() => navigate({ to: "/register" })} variant="secondary" className="text-nowrap">
            Get started
          </Button>
        </div>
      </div>
      <div className="flex flex-col items-center gap-4 py-12 px-8 md:px-48 text-white text-center">
        <FeatureTag />
        <ProductSubtitle />
        <ProductTitle />
        <ProductDescription />
        <ActionButtons />
      </div>
      <div className=" px-24 justify-center flex">
        <img
          className="hidden md:block"
          src={heroimgDesktop}
          alt="Footer"
        />
        <img className="md:hidden" src={heroimgMobile} alt="Footer" />
      </div>
    </div>
  );
}

// ProductLink: A functional component that displays the product link with dropdown
function ProductLink() {
  return (
    <DialogTrigger>
      <Button variant="icon" className="text-gray-700 dark:text-zinc-300">
        Product <ChevronDownIcon />
      </Button>
      <Popover>
        <Dialog className="flex flex-col">
          <Link href="/">Product</Link>
          <Link href="/">Product</Link>
          <Link href="/">Product</Link>
          <Link href="/">Product</Link>
        </Dialog>
      </Popover>
    </DialogTrigger>
  );
}

// ResourcesLink: A functional component that displays the resources link with dropdown
function ResourcesLink() {
  return (
    <DialogTrigger>
      <Button variant="icon" className="text-gray-700 dark:text-zinc-300">
        Resources <ChevronDownIcon />
      </Button>
      <Popover>
        <Dialog className="flex flex-col">
          <Link href="/">Product</Link>
          <Link href="/">Product</Link>
          <Link href="/">Product</Link>
          <Link href="/">Product</Link>
        </Dialog>
      </Popover>
    </DialogTrigger>
  );
}

// DocumentationLink: A functional component that displays the documentation link with dropdown
function DocumentationLink() {
  return (
    <DialogTrigger>
      <Button variant="icon" className="text-gray-700 dark:text-zinc-300">
        Documentation <ChevronDownIcon />
      </Button>
      <Popover>
        <Dialog className="flex flex-col">
          <Link href="/">Product</Link>
          <Link href="/">Product</Link>
          <Link href="/">Product</Link>
          <Link href="/">Product</Link>
        </Dialog>
      </Popover>
    </DialogTrigger>
  );
}

function FeatureTag() {
  return (
    <div className="w-64 h-8 pl-1 pr-2.5 py-1 bg-white rounded-[10px] shadow border border-gray-300 justify-start items-center gap-3 inline-flex">
      <FeatureLabel />
      <FeatureText />
    </div>
  );
}

function FeatureLabel() {
  return (
    <div className="px-2 py-0.5 bg-gray-900 rounded-md border border-slate-50 justify-start items-center flex text-nowrap">
      <div className="text-center text-slate-50 text-sm font-medium leading-tight">
        New feature
      </div>
    </div>
  );
}

function FeatureText() {
  return (
    <div className="justify-start items-center gap-1 flex">
      <div className="flex whitespace-nowrap items-center gap-2 text-slate-700 text-sm font-medium leading-tight">
        New feature here <ArrowRightIcon className="h-4 w-4" />
      </div>
      <div className="w-4 h-4 relative" />
    </div>
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
  return <h4 className="text-base font-semibold ">Super. Simple. SaaS.</h4>;
}

function ProductDescription() {
  return (
    <p className="text-gray-300 text-xl">
      Free, Open-Source .NET, React and Infrastructure kit for Startup and
      Enterprise.
    </p>
  );
}

function ActionButtons() {
  const navigate = useNavigate();
  return (
    <div className="flex justify-center gap-4">
      <Button variant="ghost" className="text-nowrap">
        Book a demo
      </Button>
      <Button onPress={() => navigate({ to: "/register" })} variant="secondary" className="text-nowrap">
        Get started today
      </Button>
    </div>
  );
}
