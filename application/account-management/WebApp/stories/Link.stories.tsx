import type { Meta } from "./Meta";
import { Link } from "@/ui/components/Link";

const meta: Meta<typeof Link> = {
  component: Link,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;

export function Example(args: any) {
  return (
    <Link {...args}>
      The missing link
    </Link>
  );
}

Example.args = {
  href: "https://www.imdb.com/title/tt6348138/",
  target: "_blank",
};
