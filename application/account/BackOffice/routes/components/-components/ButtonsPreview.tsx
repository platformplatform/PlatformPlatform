import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { MailIcon, PlusIcon, SearchIcon, TrashIcon } from "lucide-react";

import { AvatarPreview } from "./AvatarPreview";
import { ButtonGroupPreview } from "./ButtonGroupPreview";
import { LinkPreview } from "./LinkPreview";
import { SplitButtons } from "./SplitButtons";
import { TogglesPreview } from "./TogglesPreview";

export function ButtonsPreview() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Button variants</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-3">
          <Button>
            <Trans>Default</Trans>
          </Button>
          <Button variant="secondary">
            <Trans>Secondary</Trans>
          </Button>
          <Button variant="outline">
            <Trans>Outline</Trans>
          </Button>
          <Button variant="ghost">
            <Trans>Ghost</Trans>
          </Button>
          <Button variant="destructive">
            <Trans>Destructive</Trans>
          </Button>
          <Button variant="link">
            <Trans>Link</Trans>
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Button sizes</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-3">
          <Button size="xs">
            <Trans>Extra small</Trans>
          </Button>
          <Button size="sm">
            <Trans>Small</Trans>
          </Button>
          <Button size="default">
            <Trans>Default</Trans>
          </Button>
          <Button size="lg">
            <Trans>Large</Trans>
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>With icons</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-3">
          <Button>
            <PlusIcon />
            <Trans>Create</Trans>
          </Button>
          <Button variant="secondary">
            <MailIcon />
            <Trans>Send invite</Trans>
          </Button>
          <Button variant="outline">
            <SearchIcon />
            <Trans>Search</Trans>
          </Button>
          <Button variant="destructive">
            <TrashIcon />
            <Trans>Delete</Trans>
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Icon-only buttons</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-3">
          <Button size="icon-xs">
            <PlusIcon />
          </Button>
          <Button size="icon-sm">
            <PlusIcon />
          </Button>
          <Button size="icon">
            <PlusIcon />
          </Button>
          <Button size="icon-lg">
            <PlusIcon />
          </Button>
          <Button size="icon" variant="outline">
            <SearchIcon />
          </Button>
          <Button size="icon" variant="ghost">
            <TrashIcon />
          </Button>
          <Button size="icon" variant="destructive">
            <TrashIcon />
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Disabled buttons</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-3">
          <Button disabled>
            <Trans>Default</Trans>
          </Button>
          <Button variant="secondary" disabled>
            <Trans>Secondary</Trans>
          </Button>
          <Button variant="outline" disabled>
            <Trans>Outline</Trans>
          </Button>
          <Button variant="destructive" disabled>
            <Trans>Destructive</Trans>
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Split buttons</Trans>
        </h4>
        <SplitButtons />
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Button group</Trans>
        </h4>
        <ButtonGroupPreview />
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Toggle buttons</Trans>
        </h4>
        <TogglesPreview />
      </div>

      <LinkPreview />
      <AvatarPreview />
    </div>
  );
}
