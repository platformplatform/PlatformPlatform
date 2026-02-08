import { cva, type VariantProps } from "class-variance-authority";
import type * as React from "react";

import { cn } from "../utils";
import { Button } from "./Button";
import { Input } from "./Input";

// NOTE: This diverges from stock ShadCN to use CSS variable heights for Apple HIG compliance,
// explicit bg-white background, outline-based focus ring, and removed combobox context styles.
function InputGroup({ className, ...props }: React.ComponentProps<"div">) {
  return (
    // biome-ignore lint/a11y/useSemanticElements: Stock ShadCN component uses div with role="group" for styling flexibility
    <div
      data-slot="input-group"
      role="group"
      className={cn(
        "group/input-group relative flex h-[var(--control-height)] w-full min-w-0 items-center rounded-md border border-input bg-white shadow-xs outline-ring transition-[color,box-shadow] focus-within:outline focus-within:outline-2 focus-within:outline-offset-2 has-[>[data-align=block-end]]:h-auto has-[>[data-align=block-start]]:h-auto has-[>textarea]:h-auto has-[>[data-align=block-end]]:flex-col has-[>[data-align=block-start]]:flex-col has-[[data-slot][aria-invalid=true]]:border-destructive has-[[data-slot][aria-invalid=true]]:ring-[0.1875rem] has-[[data-slot][aria-invalid=true]]:ring-destructive/20 dark:bg-input/30 dark:has-[[data-slot][aria-invalid=true]]:ring-destructive/40 has-[>[data-align=block-end]]:[&>input]:pt-3 has-[>[data-align=inline-end]]:[&>input]:pr-1.5 has-[>[data-align=block-start]]:[&>input]:pb-3 has-[>[data-align=inline-start]]:[&>input]:pl-1.5",
        className
      )}
      {...props}
    />
  );
}

const inputGroupAddonVariants = cva(
  "flex h-auto cursor-text select-none items-center justify-center gap-2 py-1.5 font-medium text-muted-foreground text-sm group-data-[disabled=true]/input-group:opacity-50 [&>kbd]:rounded-[calc(var(--radius)-0.3125rem)] [&>svg:not([class*='size-'])]:size-4",
  {
    variants: {
      align: {
        "inline-start": "order-first pl-2 has-[>button]:ml-[-0.25rem] has-[>kbd]:ml-[-0.15rem]",
        "inline-end": "order-last pr-2 has-[>button]:mr-[-0.25rem] has-[>kbd]:mr-[-0.15rem]",
        "block-start":
          "order-first w-full justify-start px-2.5 pt-2 group-has-[>input]/input-group:pt-2 [.border-b]:pb-2",
        "block-end": "order-last w-full justify-start px-2.5 pb-2 group-has-[>input]/input-group:pb-2 [.border-t]:pt-2"
      }
    },
    defaultVariants: {
      align: "inline-start"
    }
  }
);

function InputGroupAddon({
  className,
  align = "inline-start",
  ...props
}: React.ComponentProps<"div"> & VariantProps<typeof inputGroupAddonVariants>) {
  return (
    // biome-ignore lint/a11y/useSemanticElements: Stock ShadCN component uses div with role="group" for styling flexibility
    // biome-ignore lint/a11y/useKeyWithClickEvents: Stock ShadCN component onClick focuses input - keyboard users navigate directly to input
    <div
      role="group"
      data-slot="input-group-addon"
      data-align={align}
      className={cn(inputGroupAddonVariants({ align }), className)}
      onClick={(e) => {
        if ((e.target as HTMLElement).closest("button")) {
          return;
        }
        e.currentTarget.parentElement?.querySelector("input")?.focus();
      }}
      {...props}
    />
  );
}

// NOTE: This diverges from stock ShadCN to use CSS variable heights and explicit dimensions
// instead of size-N shorthand for better control over min-width constraints.
const inputGroupButtonVariants = cva("flex items-center gap-2 text-sm shadow-none", {
  variants: {
    size: {
      xs: "h-[var(--control-height-xs)] gap-1 rounded-[calc(var(--radius)-0.3125rem)] px-1.5 [&>svg:not([class*='size-'])]:size-3.5",
      sm: "",
      "icon-xs": "h-7 w-7 min-w-7 rounded-[calc(var(--radius)-0.3125rem)] p-0 has-[>svg]:p-0",
      "icon-sm": "h-9 w-9 min-w-9 p-0 has-[>svg]:p-0"
    }
  },
  defaultVariants: {
    size: "xs"
  }
});

function InputGroupButton({
  className,
  type = "button",
  variant = "ghost",
  size = "xs",
  ...props
}: Omit<React.ComponentProps<typeof Button>, "size" | "type"> &
  VariantProps<typeof inputGroupButtonVariants> & {
    type?: "button" | "submit" | "reset";
  }) {
  return (
    <Button
      type={type}
      data-size={size}
      variant={variant}
      className={cn(inputGroupButtonVariants({ size }), className)}
      {...props}
    />
  );
}

function InputGroupText({ className, ...props }: React.ComponentProps<"span">) {
  return (
    <span
      className={cn(
        "flex items-center gap-2 text-muted-foreground text-sm [&_svg:not([class*='size-'])]:size-4 [&_svg]:pointer-events-none",
        className
      )}
      {...props}
    />
  );
}

// NOTE: This diverges from stock ShadCN to suppress the inner input focus ring,
// since the parent InputGroup handles focus-within outline styling.
function InputGroupInput({ className, ...props }: React.ComponentProps<"input">) {
  return (
    <Input
      data-slot="input-group-control"
      className={cn(
        "flex-1 rounded-none border-0 bg-transparent shadow-none ring-0 ring-offset-0 focus-visible:outline-none focus-visible:ring-0 focus-visible:ring-offset-0 aria-invalid:ring-0 dark:bg-transparent",
        className
      )}
      {...props}
    />
  );
}

export { InputGroup, InputGroupAddon, InputGroupButton, InputGroupText, InputGroupInput };
