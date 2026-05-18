import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
  CommandShortcut
} from "@repo/ui/components/Command";
import { Kbd, KbdGroup } from "@repo/ui/components/Kbd";
import { CalendarIcon, FileIcon, SettingsIcon, SmileIcon, UserIcon } from "lucide-react";
import { useEffect, useState } from "react";

export function CommandPreview() {
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "k" && (event.metaKey || event.ctrlKey)) {
        event.preventDefault();
        setOpen((previous) => !previous);
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  return (
    <section className="flex flex-col gap-3">
      <h3>
        <Trans>Command</Trans>
      </h3>
      <p className="text-sm text-muted-foreground">
        <Trans>
          Search-driven menu with grouped items and inline shortcuts. Wrap it in CommandDialog to surface as a Cmd+K
          palette.
        </Trans>
      </p>
      <div className="flex items-center gap-3">
        <Button variant="outline" onClick={() => setOpen(true)}>
          <Trans>Open command palette</Trans>
        </Button>
        <span className="flex items-center gap-2 text-sm text-muted-foreground">
          <Trans>or press</Trans>
          <KbdGroup>
            <Kbd>⌘</Kbd>
            <Kbd>K</Kbd>
          </KbdGroup>
        </span>
      </div>
      <CommandDialog
        open={open}
        onOpenChange={setOpen}
        trackingTitle="Command palette"
        title={t`Command palette`}
        description={t`Search and run commands`}
        className="sm:w-dialog-md"
      >
        <CommandInput placeholder={t`Type a command or search...`} />
        <CommandList>
          <CommandEmpty>
            <Trans>No results found.</Trans>
          </CommandEmpty>
          <CommandGroup heading={t`Suggestions`}>
            <CommandItem onSelect={() => setOpen(false)}>
              <CalendarIcon />
              <Trans>Calendar</Trans>
            </CommandItem>
            <CommandItem onSelect={() => setOpen(false)}>
              <SmileIcon />
              <Trans>Search emoji</Trans>
            </CommandItem>
            <CommandItem onSelect={() => setOpen(false)}>
              <FileIcon />
              <Trans>New document</Trans>
              <CommandShortcut>⌘N</CommandShortcut>
            </CommandItem>
          </CommandGroup>
          <CommandSeparator />
          <CommandGroup heading={t`Settings`}>
            <CommandItem onSelect={() => setOpen(false)}>
              <UserIcon />
              <Trans>Profile</Trans>
              <CommandShortcut>⌘P</CommandShortcut>
            </CommandItem>
            <CommandItem onSelect={() => setOpen(false)}>
              <SettingsIcon />
              <Trans>Preferences</Trans>
              <CommandShortcut>⌘,</CommandShortcut>
            </CommandItem>
          </CommandGroup>
        </CommandList>
      </CommandDialog>
    </section>
  );
}
