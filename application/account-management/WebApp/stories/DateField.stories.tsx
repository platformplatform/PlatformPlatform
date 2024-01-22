import type { Meta } from "@storybook/react";
import { Form } from "react-aria-components";
import { Button } from "@/ui/components/Button";
import { DateField } from "@/ui/components/DateField";

const meta: Meta<typeof DateField> = {
  component: DateField,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
  args: {
    label: "Event date",
  },
};

export default meta;

export const Example = (args: any) => <DateField {...args} />;

export function Validation(args: any) {
  return (
    <Form className="flex flex-col gap-2 items-start">
      <DateField {...args} />
      <Button type="submit" variant="secondary">Submit</Button>
    </Form>
  );
}

Validation.args = {
  isRequired: true,
};
