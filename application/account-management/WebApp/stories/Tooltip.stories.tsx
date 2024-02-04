import { PrinterIcon, SaveIcon } from "lucide-react";
import { TooltipTrigger } from "react-aria-components";
import type { Meta } from "./Meta";
import { Button } from "@/ui/components/Button";
import { Tooltip } from "@/ui/components/Tooltip";

const meta: Meta<typeof Tooltip> = {
  component: Tooltip,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;

export function Example(args: any) {
  return (
    <div className="flex gap-2">
      <TooltipTrigger>
        <Button variant="secondary" className="px-2"><SaveIcon className="w-5 h-5" /></Button>
        <Tooltip {...args}>Save</Tooltip>
      </TooltipTrigger>
      <TooltipTrigger>
        <Button variant="secondary" className="px-2"><PrinterIcon className="w-5 h-5" /></Button>
        <Tooltip {...args}>Print</Tooltip>
      </TooltipTrigger>
    </div>
  );
}
