import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage
} from "@repo/ui/components/Breadcrumb";
import {
  ContextMenu,
  ContextMenuCheckboxItem,
  ContextMenuContent,
  ContextMenuGroup,
  ContextMenuItem,
  ContextMenuLabel,
  ContextMenuRadioGroup,
  ContextMenuRadioItem,
  ContextMenuSeparator,
  ContextMenuShortcut,
  ContextMenuSub,
  ContextMenuSubContent,
  ContextMenuSubTrigger,
  ContextMenuTrigger
} from "@repo/ui/components/ContextMenu";
import { Link } from "@repo/ui/components/Link";
import { SlashIcon } from "lucide-react";
import { useState } from "react";

import { CommandPreview } from "./CommandPreview";
import { KbdPreview } from "./KbdPreview";
import { LinkCardPreview } from "./LinkCardPreview";
import { NavigationMenuPreview } from "./NavigationMenuPreview";

export function NavigationPreview() {
  const [showBookmarks, setShowBookmarks] = useState(true);
  const [showStatusBar, setShowStatusBar] = useState(false);
  const [person, setPerson] = useState("pedro");

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Breadcrumb — simple</Trans>
        </h4>
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink render={<Link href="/" variant="secondary" underline={false} />}>
                <Trans>Home</Trans>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbItem>
              <BreadcrumbPage>
                <Trans>Components</Trans>
              </BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Breadcrumb — multi-level with auto-collapse</Trans>
        </h4>
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink render={<Link href="/" variant="secondary" underline={false} />}>
                <Trans>Home</Trans>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbItem>
              <BreadcrumbLink render={<Link href="/" variant="secondary" underline={false} />}>
                <Trans>Library</Trans>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbItem>
              <BreadcrumbLink render={<Link href="/" variant="secondary" underline={false} />}>
                <Trans>Data</Trans>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbItem>
              <BreadcrumbPage>
                <Trans>Current page</Trans>
              </BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Breadcrumb — custom separator</Trans>
        </h4>
        <Breadcrumb>
          <BreadcrumbList separator={<SlashIcon />}>
            <BreadcrumbItem>
              <BreadcrumbLink render={<Link href="/" variant="secondary" underline={false} />}>
                <Trans>Home</Trans>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbItem>
              <BreadcrumbPage>
                <Trans>Components</Trans>
              </BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Context menu</Trans>
        </h4>
        <ContextMenu>
          <ContextMenuTrigger
            className="flex h-32 w-full items-center justify-center rounded-md border border-dashed text-sm text-muted-foreground"
            aria-label={t`Right-click area`}
          >
            <Trans>Right-click here</Trans>
          </ContextMenuTrigger>
          <ContextMenuContent>
            <ContextMenuItem>
              <Trans>Back</Trans>
              <ContextMenuShortcut>⌘[</ContextMenuShortcut>
            </ContextMenuItem>
            <ContextMenuItem disabled={true}>
              <Trans>Forward</Trans>
              <ContextMenuShortcut>⌘]</ContextMenuShortcut>
            </ContextMenuItem>
            <ContextMenuItem>
              <Trans>Reload</Trans>
              <ContextMenuShortcut>⌘R</ContextMenuShortcut>
            </ContextMenuItem>
            <ContextMenuSub>
              <ContextMenuSubTrigger>
                <Trans>More tools</Trans>
              </ContextMenuSubTrigger>
              <ContextMenuSubContent>
                <ContextMenuItem>
                  <Trans>Save page as...</Trans>
                </ContextMenuItem>
                <ContextMenuItem>
                  <Trans>Create shortcut...</Trans>
                </ContextMenuItem>
                <ContextMenuItem>
                  <Trans>Developer tools</Trans>
                </ContextMenuItem>
              </ContextMenuSubContent>
            </ContextMenuSub>
            <ContextMenuSeparator />
            <ContextMenuCheckboxItem checked={showBookmarks} onCheckedChange={setShowBookmarks}>
              <Trans>Show bookmarks</Trans>
              <ContextMenuShortcut>⌘⇧B</ContextMenuShortcut>
            </ContextMenuCheckboxItem>
            <ContextMenuCheckboxItem checked={showStatusBar} onCheckedChange={setShowStatusBar}>
              <Trans>Show status bar</Trans>
            </ContextMenuCheckboxItem>
            <ContextMenuSeparator />
            <ContextMenuGroup>
              <ContextMenuLabel>
                <Trans>People</Trans>
              </ContextMenuLabel>
              <ContextMenuRadioGroup value={person} onValueChange={setPerson}>
                <ContextMenuRadioItem value="pedro">
                  <Trans>Pedro Duarte</Trans>
                </ContextMenuRadioItem>
                <ContextMenuRadioItem value="colm">
                  <Trans>Colm Tuite</Trans>
                </ContextMenuRadioItem>
              </ContextMenuRadioGroup>
            </ContextMenuGroup>
            <ContextMenuSeparator />
            <ContextMenuItem variant="destructive">
              <Trans>Delete</Trans>
              <ContextMenuShortcut>⌫</ContextMenuShortcut>
            </ContextMenuItem>
          </ContextMenuContent>
        </ContextMenu>
      </div>
      <LinkCardPreview />
      <NavigationMenuPreview />
      <CommandPreview />
      <KbdPreview />
    </div>
  );
}
