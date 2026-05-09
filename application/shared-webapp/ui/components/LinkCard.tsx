import { Link, type LinkComponentProps } from "@tanstack/react-router";

import { cn } from "../utils";

// A navigable card. Combines Card's visual styling with a TanStack Router <Link> so the
// whole card is the focusable element — keyboard focus ring lands on the card itself
// (via the global `a:focus-visible` rule in tailwind.css) without being clipped by a
// surrounding `<Card>`'s overflow-hidden.
export function LinkCard({ className, ...props }: LinkComponentProps) {
  return (
    <Link
      data-slot="card"
      className={cn(
        "flex min-w-0 flex-col gap-6 rounded-xl border bg-card p-6 text-card-foreground shadow-sm transition-[background-color] hover:bg-accent active:bg-muted [&_*]:break-words [&>*]:max-w-full [&>*]:min-w-0",
        className
      )}
      {...props}
    />
  );
}
