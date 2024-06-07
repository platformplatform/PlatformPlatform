const youtubeImageUrl = "https://platformplatformgithub.blob.core.windows.net/youtube.svg?url";
const slackImageUrl = "https://platformplatformgithub.blob.core.windows.net/slack.svg?url";
const githubImageUrl = "https://platformplatformgithub.blob.core.windows.net/github.svg?url";
const leftArrowImageUrl = "https://platformplatformgithub.blob.core.windows.net/left-arrow.svg?url";

// Props for FeatureBlock component
interface FeatureBlockProps {
  title: string;
  content: string;
  image: string;
  linker: string;
  linkText: string;
  arrow?: string;
}

// Component to display a feature with an image, title, content, and a link
function FeatureBlock({ title, content, image, linker, linkText, arrow }: FeatureBlockProps) {
  return (
    <div className="flex flex-col gap-4  items-center justify-center">
      <div className="flex shadow rounded-lg w-12 h-12 justify-center p-1 items-center">
        <img src={image} alt={title} />
      </div>
      <div className="text-gray-900 text-xl font-semibold text-center">{title}</div>
      <div className="text-slate-600 text-base font-normal text-center">{content}</div>
      <a href={linker} className="text-gray-700 text-base font-semibold text-nowrap hover:underline flex gap-2">
        {linkText} {arrow && <img src={arrow} alt="Arrow" />}
      </a>
    </div>
  );
}

// Component for the Community Section
export function CommunitySection() {
  return (
    <div className="flex flex-col gap-8 items-center bg-white py-48">
      <div className="flex flex-col gap-8 text-center items-center md:px-32 lg:px-64 px-8 max-w-7xl">
        <h1 className="text-amber-600 text-base font-semibold">COMMUNITY</h1>
        <h2 className="text-gray-900 text-4xl md:text-6xl font-semibold">Join builders on PlatformPlatform</h2>
        <p className="text-slate-600 text-xl font-normal">
          Our community is full of developers, designers and founders -
          just like you, to get your going, sharing ideas and experiences. Join us now.
        </p>
      </div>
      <div className="grid grid-row-3 md:grid-cols-3 px-32 gap-16">
        <FeatureBlock
          title="YouTube"
          content="Subscribe to our YouTube channel to get quickly started, and stay informed of new features."
          image={youtubeImageUrl}
          linker="https://www.youtube.com"
          linkText="View training videos"
          arrow={leftArrowImageUrl}
        />
        <FeatureBlock
          title="Slack"
          content="Chat with our team or other members of the PlatfomaximusrmPlatform community."
          image={slackImageUrl}
          linker="https://slack.com"
          linkText="Join our Slack channel"
          arrow={leftArrowImageUrl}
        />
        <FeatureBlock
          title="GitHub"
          content="View our GitHub repository and collaborate with thousands of developers like you."
          image={githubImageUrl}
          linker="https://github.com"
          linkText="Check GitHub"
          arrow={leftArrowImageUrl}
        />
      </div>
    </div>
  );
}
