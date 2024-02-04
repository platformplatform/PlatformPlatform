import { Form } from "react-aria-components";
import type { Meta } from "./Meta";
import { Button } from "@/ui/components/Button";
import { SearchField } from "@/ui/components/SearchField";

const meta: Meta<typeof SearchField> = {
  component: SearchField,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
  args: {
    label: "Search",
  },
};

export default meta;

export const Example = (args: any) => <SearchField {...args} />;

export function Validation(args: any) {
  return (
    <Form className="flex flex-col gap-2 items-start">
      <SearchField {...args} />
      <Button type="submit" variant="secondary">Submit</Button>
    </Form>
  );
}

Validation.args = {
  isRequired: true,
};
