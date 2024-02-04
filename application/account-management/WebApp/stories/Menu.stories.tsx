import { MoreHorizontal } from "lucide-react";
import { MenuTrigger } from "react-aria-components";
import type { Meta } from "./Meta";
import { Button } from "@/ui/components/Button";
import { Menu, MenuItem, MenuSection, MenuSeparator } from "@/ui/components/Menu";

const meta: Meta<typeof Menu> = {
  component: Menu,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;

export function Example(args: any) {
  return (
    <MenuTrigger>
      <Button variant="secondary" className="px-2">
        <MoreHorizontal className="w-5 h-5" />
      </Button>
      <Menu {...args}>
        <MenuItem id="new">New…</MenuItem>
        <MenuItem id="open">Open…</MenuItem>
        <MenuSeparator />
        <MenuItem id="save">Save</MenuItem>
        <MenuItem id="saveAs">Save as…</MenuItem>
        <MenuSeparator />
        <MenuItem id="print">Print…</MenuItem>
      </Menu>
    </MenuTrigger>
  );
}

export const DisabledItems = (args: any) => <Example {...args} />;
DisabledItems.args = {
  disabledKeys: ["save"],
};

export function Sections(args: any) {
  return (
    <MenuTrigger>
      <Button variant="secondary" className="px-2">
        <MoreHorizontal className="w-5 h-5" />
      </Button>
      <Menu {...args}>
        <MenuSection title="Your Content">
          <MenuItem id="repos">Repositories</MenuItem>
          <MenuItem id="projects">Projects</MenuItem>
          <MenuItem id="organizations">Organizations</MenuItem>
          <MenuItem id="stars">Stars</MenuItem>
          <MenuItem id="sponsors">Sponsors</MenuItem>
        </MenuSection>
        <MenuSection title="Your Account">
          <MenuItem id="profile">Profile</MenuItem>
          <MenuItem id="status">Set status</MenuItem>
          <MenuItem id="sign-out">Sign out</MenuItem>
        </MenuSection>
      </Menu>
    </MenuTrigger>
  );
}
