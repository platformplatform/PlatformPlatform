import { Link } from "@repo/ui/components/Link";
import { ArrowRight } from "lucide-react";
import type { LinkProps } from "react-aria-components";
import { githubImageUrl, slackImageUrl, youtubeImageUrl } from "./cdnImages";

// Props for FeatureBlock component
interface FeatureBlockProps {
  title: string;
  content: string;
  image: string;
  href: LinkProps["href"];
  linkText: string;
  arrow?: boolean;
}

// Component to display a feature with an image, title, content, and a link
function FeatureBlock({ title, content, image, href, linkText, arrow }: Readonly<FeatureBlockProps>) {
  return (
    <div className="flex flex-col items-center justify-center gap-4">
      <div className="flex h-12 w-12 items-center justify-center rounded-lg p-1 shadow">
        <img src={image} alt={title} />
      </div>
      <div className="text-center font-semibold text-foreground text-xl">{title}</div>
      <div className="text-center font-normal text-base text-muted-foreground">{content}</div>
      <Link href={href}>
        {linkText} {arrow && <ArrowRight className="h-4 w-4" />}
      </Link>
    </div>
  );
}

// Component for the Community Section
export function CommunitySection() {
  return (
    <div className="flex flex-col items-center gap-8 bg-background py-48">
      <div className="flex max-w-7xl flex-col items-center gap-8 px-8 text-center md:px-32 lg:px-64">
        <h1 className="font-semibold text-amber-600 text-base">COMMUNITY</h1>
        <h2 className="font-semibold text-4xl text-foreground md:text-6xl">Join builders on PlatformPlatform</h2>
        <p className="font-normal text-muted-foreground text-xl">
          Our community is full of developers, designers and founders - just like you, to get your going, sharing ideas
          and experiences. Join us now.
        </p>
      </div>
      <div className="grid-row-3 grid gap-16 px-32 md:grid-cols-3">
        <FeatureBlock
          title="YouTube"
          content="Subscribe to our YouTube channel to get quickly started, and stay informed of new features."
          image={youtubeImageUrl}
          href="https://www.youtube.com"
          linkText="View training videos"
          arrow={true}
        />
        <FeatureBlock
          title="Slack"
          content="Chat with our team or other members of the PlatformPlatform community."
          image={slackImageUrl}
          href="https://slack.com"
          linkText="Join our Slack channel"
          arrow={true}
        />
        <FeatureBlock
          title="GitHub"
          content="View our GitHub repository and collaborate with thousands of developers like you."
          image={githubImageUrl}
          href="https://github.com"
          linkText="Check GitHub"
          arrow={true}
        />
      </div>
    </div>
  );
}
