"use client";
import { CtaSection } from "./_landing/sections/CtaSection";
import { FeatureSection } from "./_landing/sections/FeatureSection";
import { FeatureSection2 } from "./_landing/sections/FeatureSection2";
import { HeroSection } from "./_landing/sections/HeroSection";
import { FeatureSection3 } from "./_landing/sections/FeatureSection3";
import { TechnologySection } from "./_landing/sections/TechnologySection";
import { TechnologySection2 } from "./_landing/sections/TechnologySection2";
import { CommunitySection } from "./_landing/sections/CommunitySection";
import { FeatureSection4 } from "./_landing/sections/FeatureSection4";
import { CtaSection2 } from "./_landing/sections/CtaSection2";
import { CtaSection3 } from "./_landing/sections/CtaSection3";
import { FooterSection } from "./_landing/sections/FooterSection";

export default function LandingPage() {
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
