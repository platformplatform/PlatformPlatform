import { BoldIcon, ItalicIcon, UnderlineIcon } from "lucide-react";
import { Group } from "react-aria-components";
import type { Meta } from "./Meta";
import { Button } from "@/ui/components/Button";
import { Checkbox } from "@/ui/components/Checkbox";
import { Separator } from "@/ui/components/Separator";
import { ToggleButton } from "@/ui/components/ToggleButton";
import { Toolbar } from "@/ui/components/Toolbar";

const meta: Meta<typeof Toolbar> = {
  component: Toolbar,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;

export function Example(args: any) {
  return (
    <Toolbar aria-label="Text formatting" {...args}>
      <Group aria-label="Style" className="contents">
        <ToggleButton aria-label="Bold" className="p-2.5">
          <BoldIcon className="w-4 h-4" />
        </ToggleButton>
        <ToggleButton aria-label="Italic" className="p-2.5">
          <ItalicIcon className="w-4 h-4" />
        </ToggleButton>
        <ToggleButton aria-label="Underline" className="p-2.5">
          <UnderlineIcon className="w-4 h-4" />
        </ToggleButton>
      </Group>
      <Separator orientation={args.orientation === "vertical" ? "horizontal" : "vertical"} />
      <Group aria-label="Clipboard" className="contents">
        <Button variant="secondary">Copy</Button>
        <Button variant="secondary">Paste</Button>
        <Button variant="secondary">Cut</Button>
      </Group>
      <Separator orientation={args.orientation === "vertical" ? "horizontal" : "vertical"} />
      <Checkbox>Night Mode</Checkbox>
    </Toolbar>
  );
}
