import type { Meta } from "@storybook/react";
import { Form } from "react-aria-components";
import { Button } from "@/ui/components/Button";
import { TimeField } from "@/ui/components/TimeField";

const meta: Meta<typeof TimeField> = {
  component: TimeField,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
  args: {
    label: "Event time",
  },
};

export default meta;

export const Example = (args: any) => <TimeField {...args} />;

export function Validation(args: any) {
  return (
    <Form className="flex flex-col gap-2 items-start">
      <TimeField {...args} />
      <Button type="submit" variant="secondary">Submit</Button>
    </Form>
  );
}

Validation.args = {
  isRequired: true,
};
