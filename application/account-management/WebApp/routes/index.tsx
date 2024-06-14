import { createFileRoute } from "@tanstack/react-router";
import { CtaSection } from "./-landing/sections/CtaSection";
import { FeatureSection } from "./-landing/sections/FeatureSection";
import { FeatureSection2 } from "./-landing/sections/FeatureSection2";
import { HeroSection } from "./-landing/sections/HeroSection";
import { FeatureSection3 } from "./-landing/sections/FeatureSection3";
import { TechnologySection } from "./-landing/sections/TechnologySection";
import { TechnologySection2 } from "./-landing/sections/TechnologySection2";
import { CommunitySection } from "./-landing/sections/CommunitySection";
import { FeatureSection4 } from "./-landing/sections/FeatureSection4";
import { CtaSection2 } from "./-landing/sections/CtaSection2";
import { CtaSection3 } from "./-landing/sections/CtaSection3";
import { FooterSection } from "./-landing/sections/FooterSection";

export const Route = createFileRoute("/")({
  component: LandingPage,
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
