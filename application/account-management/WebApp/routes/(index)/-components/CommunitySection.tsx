import { Link } from "@repo/ui/components/Link";
import { ArrowRight } from "lucide-react";
import type { LinkProps } from "react-aria-components";

const youtubeImageUrl = "https://platformplatformgithub.blob.core.windows.net/youtube.svg?url";
const slackImageUrl = "https://platformplatformgithub.blob.core.windows.net/slack.svg?url";
const githubImageUrl = "https://platformplatformgithub.blob.core.windows.net/github.svg?url";

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
    <div className="flex flex-col gap-4  items-center justify-center">
      <div className="flex shadow rounded-lg w-12 h-12 justify-center p-1 items-center">
        <img src={image} alt={title} />
      </div>
      <div className="text-foreground text-xl font-semibold text-center">{title}</div>
      <div className="text-muted-foreground text-base font-normal text-center">{content}</div>
      <Link href={href}>
        {linkText} {arrow && <ArrowRight className="w-4 h-4" />}
      </Link>
    </div>
  );
}

// Component for the Community Section
export function CommunitySection() {
  return (
    <div className="flex flex-col gap-8 items-center bg-background py-48">
      <div className="flex flex-col gap-8 text-center items-center md:px-32 lg:px-64 px-8 max-w-7xl">
        <h1 className="text-amber-600 text-base font-semibold">COMMUNITY</h1>
        <h2 className="text-foreground text-4xl md:text-6xl font-semibold">Join builders on PlatformPlatform</h2>
        <p className="text-muted-foreground text-xl font-normal">
          Our community is full of developers, designers and founders - just like you, to get your going, sharing ideas
          and experiences. Join us now.
        </p>
      </div>
      <div className="grid grid-row-3 md:grid-cols-3 px-32 gap-16">
        <FeatureBlock
          title="YouTube"
          content="Subscribe to our YouTube channel to get quickly started, and stay informed of new features."
          image={youtubeImageUrl}
          href="https://www.youtube.com"
          linkText="View training videos"
          arrow
        />
        <FeatureBlock
          title="Slack"
          content="Chat with our team or other members of the PlatformPlatform community."
          image={slackImageUrl}
          href="https://slack.com"
          linkText="Join our Slack channel"
          arrow
        />
        <FeatureBlock
          title="GitHub"
          content="View our GitHub repository and collaborate with thousands of developers like you."
          image={githubImageUrl}
          href="https://github.com"
          linkText="Check GitHub"
          arrow
        />
      </div>
    </div>
  );
}
