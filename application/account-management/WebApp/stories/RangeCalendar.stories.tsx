import type { Meta } from "@storybook/react";
import { RangeCalendar } from "@/ui/components/RangeCalendar";

const meta: Meta<typeof RangeCalendar> = {
  component: RangeCalendar,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;

export function Example(args: any) {
  return <RangeCalendar aria-label="Trip dates" {...args} />;
}
