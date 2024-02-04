import type { Meta } from "./Meta";
import { Breadcrumb, Breadcrumbs } from "@/ui/components/Breadcrumbs";

const meta: Meta<typeof Breadcrumbs> = {
  component: Breadcrumbs,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;

export function Example(args: any) {
  return (
    <Breadcrumbs {...args}>
      <Breadcrumb href="/">Home</Breadcrumb>
      <Breadcrumb href="/react-aria">React Aria</Breadcrumb>
      <Breadcrumb>Breadcrumbs</Breadcrumb>
    </Breadcrumbs>
  );
}
