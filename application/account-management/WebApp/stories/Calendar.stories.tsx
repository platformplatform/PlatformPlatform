import type { Meta } from "./Meta";
import { Calendar } from "@/ui/components/Calendar";

const meta: Meta<typeof Calendar> = {
  component: Calendar,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;

export function Example(args: any) {
  return <Calendar aria-label="Event date" {...args} />;
}
