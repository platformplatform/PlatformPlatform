import type { Meta } from "./Meta";
import { Button } from "@/ui/components/Button";
import { DateField } from "@/ui/components/DateField";
import { Form } from "@/ui/components/Form";
import { TextField } from "@/ui/components/TextField";

const meta: Meta<typeof Form> = {
  component: Form,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;

export function Example(args: any) {
  return (
    <Form {...args}>
      <TextField label="Email" name="email" type="email" isRequired />
      <DateField label="Birth date" isRequired />
      <div className="flex gap-2">
        <Button type="submit">Submit</Button>
        <Button type="reset" variant="secondary">Reset</Button>
      </div>
    </Form>
  );
}
