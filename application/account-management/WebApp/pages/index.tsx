import { createFileRoute } from "@tanstack/react-router";
import { CtaSection } from "../shared/ui/landingPage/sections/CtaSection";
import { FeatureSection } from "../shared/ui/landingPage/sections/FeatureSection";
import { FeatureSection2 } from "../shared/ui/landingPage/sections/FeatureSection2";
import { HeroSection } from "../shared/ui/landingPage/sections/HeroSection";
import { FeatureSection3 } from "../shared/ui/landingPage/sections/FeatureSection3";
import { TechnologySection } from "../shared/ui/landingPage/sections/TechnologySection";
import { TechnologySection2 } from "../shared/ui/landingPage/sections/TechnologySection2";
import { CommunitySection } from "../shared/ui/landingPage/sections/CommunitySection";
import { FeatureSection4 } from "../shared/ui/landingPage/sections/FeatureSection4";
import { CtaSection2 } from "../shared/ui/landingPage/sections/CtaSection2";
import { CtaSection3 } from "../shared/ui/landingPage/sections/CtaSection3";
import { FooterSection } from "../shared/ui/landingPage/sections/FooterSection";

export const Route = createFileRoute("/")({
  component: LandingPage
});

function LandingPage() {
  return (
    <main className="flex w-full flex-col">
      <HeroSection />
      <FeatureSection />
      <CtaSection />
      <FeatureSection2 />
      <FeatureSection3 />
      <TechnologySection />
      <TechnologySection2 />
      <CommunitySection />
      <FeatureSection4 />
      <CtaSection2 />
      <CtaSection3 />
      <FooterSection />
    </main>
  );
}
