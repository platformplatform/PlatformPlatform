import { Checkbox } from "../components/Checkbox";

export default {
  title: "Checkbox",
  component: Checkbox,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
  argTypes: {},
  args: {
    isDisabled: false,
    children: "Checkbox",
  },
};

export const Default = {
  args: {},
};
