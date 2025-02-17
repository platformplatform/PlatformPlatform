import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { Navigate, createFileRoute } from "@tanstack/react-router";
import { CommunitySection } from "./-components/CommunitySection";
import { CtaSection } from "./-components/CtaSection";
import { CtaSection2 } from "./-components/CtaSection2";
import { CtaSection3 } from "./-components/CtaSection3";
import { FeatureSection } from "./-components/FeatureSection";
import { FeatureSection2 } from "./-components/FeatureSection2";
import { FeatureSection3 } from "./-components/FeatureSection3";
import { FeatureSection4 } from "./-components/FeatureSection4";
import { FooterSection } from "./-components/FooterSection";
import { HeroSection } from "./-components/HeroSection";
import { TechnologySection } from "./-components/TechnologySection";
import { TechnologySection2 } from "./-components/TechnologySection2";

export const Route = createFileRoute("/(index)/")({
  component: function LandingPage() {
    const isAuthenticated = useIsAuthenticated();

    if (isAuthenticated) {
      return <Navigate to={loggedInPath} />;
    }

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
});
