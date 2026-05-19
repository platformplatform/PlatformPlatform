import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { HoverCard, HoverCardContent, HoverCardTrigger } from "@repo/ui/components/HoverCard";
import { CalendarIcon } from "lucide-react";

export function HoverCardPreview() {
  return (
    <section className="flex flex-col gap-3">
      <h3>
        <Trans>HoverCard</Trans>
      </h3>
      <p className="text-sm text-muted-foreground">
        <Trans>
          Card-style preview shown on hover or focus. Use it for user mentions, link previews, or any compact context
          that would otherwise need a click.
        </Trans>
      </p>
      <HoverCard>
        <HoverCardTrigger
          render={
            <Button variant="link" className="w-fit">
              @alex.taylor
            </Button>
          }
        />
        <HoverCardContent className="w-80">
          <div className="flex gap-3">
            <Avatar className="size-12">
              <AvatarImage src="https://i.pravatar.cc/96?img=12" alt="Alex Taylor" />
              <AvatarFallback>AT</AvatarFallback>
            </Avatar>
            <div className="flex flex-col gap-1">
              <span className="font-semibold">@alex.taylor</span>
              <span className="text-sm">
                <Trans>Tinkers with food on weekends. Recipe library curator since 2024.</Trans>
              </span>
              <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <CalendarIcon className="size-3.5" />
                <Trans>Joined March 2024</Trans>
              </span>
            </div>
          </div>
        </HoverCardContent>
      </HoverCard>
    </section>
  );
}
