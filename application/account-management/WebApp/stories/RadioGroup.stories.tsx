import { Form } from "react-aria-components";
import { Button } from "@/ui/components/Button";
import { Radio, RadioGroup } from "@/ui/components/RadioGroup";

export default {
  title: "RadioGroup",
  component: RadioGroup,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
  argTypes: {},
  args: {
    label: "Favorite sport",
    isDisabled: false,
    isRequired: false,
    description: "",
    children: <>
      <Radio value="soccer">Soccer</Radio>
      <Radio value="baseball">Baseball</Radio>
      <Radio value="basketball">Basketball</Radio>
    </>,
  },
};

export const Default = {
  args: {},
};

export function Validation(args: any) {
  return (
    <Form className="flex flex-col gap-2 items-start">
      <RadioGroup {...args} />
      <Button type="submit" variant="secondary">Submit</Button>
    </Form>
  );
}

Validation.args = {
  isRequired: true,
};
