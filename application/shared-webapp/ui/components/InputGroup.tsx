import type * as React from "react";

import { cva, type VariantProps } from "class-variance-authority";
import { createContext, useContext, useRef } from "react";

import { cn } from "../utils";
import { Button } from "./Button";
import { Input } from "./Input";

// Shared ref so InputGroupAddon can focus the InputGroupInput without DOM queries (works regardless of wrapper depth).
const InputGroupInputRefContext = createContext<React.RefObject<HTMLInputElement | null> | null>(null);

function InputGroup({ className, ...props }: React.ComponentProps<"div">) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  return (
    <InputGroupInputRefContext.Provider value={inputRef}>
      <div
        data-slot="input-group"
        role="group"
        className={cn(
          "group/input-group relative flex h-(--control-height) w-full min-w-0 items-center rounded-md border border-input bg-white shadow-xs outline-ring transition-[color,box-shadow] focus-within:outline-2 focus-within:outline-offset-2 has-[[data-slot][aria-invalid=true]]:outline-2 has-[[data-slot][aria-invalid=true]]:outline-offset-2 has-[[data-slot][aria-invalid=true]]:outline-destructive focus-within:has-[[data-slot][aria-invalid=true]]:shadow-error-halo has-[>[data-align=block-end]]:h-auto has-[>[data-align=block-end]]:flex-col has-[>[data-align=block-start]]:h-auto has-[>[data-align=block-start]]:flex-col has-[>textarea]:h-auto data-disabled:pointer-events-none data-disabled:opacity-50 dark:bg-input/30 has-[>[data-align=block-end]]:[&>input]:pt-3 has-[>[data-align=block-start]]:[&>input]:pb-3 has-[>[data-align=inline-end]]:[&>input]:pr-1.5 has-[>[data-align=inline-start]]:[&>input]:pl-1.5",
          className
        )}
        {...props}
      />
    </InputGroupInputRefContext.Provider>
  );
}

const inputGroupAddonVariants = cva(
  "flex h-auto cursor-text items-center justify-center gap-2 py-1.5 text-sm font-medium text-muted-foreground select-none [&>kbd]:rounded-[calc(var(--radius)-0.3125rem)] [&>svg:not([class*='size-'])]:size-4",
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
  const inputRef = useContext(InputGroupInputRefContext);
  return (
    // The addon only forwards focus to the fully-accessible sibling input (mirrors native <label> click-to-focus), so promoting
    // it to an interactive role would wrongly add it to the tab order.
    // oxlint-disable-next-line jsx-a11y/no-noninteractive-element-interactions
    <div
      role="group"
      data-slot="input-group-addon"
      data-align={align}
      className={cn(inputGroupAddonVariants({ align }), className)}
      // Clicking the addon area (icon slot, leading/trailing glyph) focuses the sibling input -- matches the UX of clicking
      // a <label>, since the addon is not itself a labelable element. Skip when the click lands on a button so addon buttons
      // (clear, submit) still fire their own action instead of stealing focus to the input.
      onClick={(e) => {
        if ((e.target as HTMLElement).closest("button")) {
          return;
        }
        inputRef?.current?.focus();
      }}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          inputRef?.current?.focus();
        }
      }}
      {...props}
    />
  );
}

const inputGroupButtonVariants = cva("flex items-center gap-2 text-sm shadow-none", {
  variants: {
    size: {
      xs: "h-[var(--control-height-xs)] gap-1 rounded-[calc(var(--radius)-0.3125rem)] px-1.5 [&>svg:not([class*='size-'])]:size-3.5",
      sm: "",
      "icon-xs":
        "h-[var(--control-height-xs)] w-[var(--control-height-xs)] min-w-[var(--control-height-xs)] rounded-[calc(var(--radius)-0.3125rem)] p-0 has-[>svg]:p-0",
      "icon-sm":
        "h-[var(--control-height-sm)] w-[var(--control-height-sm)] min-w-[var(--control-height-sm)] p-0 has-[>svg]:p-0"
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
        "flex items-center gap-2 text-sm text-muted-foreground [&_svg]:pointer-events-none [&_svg:not([class*='size-'])]:size-4",
        className
      )}
      {...props}
    />
  );
}

function InputGroupInput({ className, ref, ...props }: React.ComponentProps<"input">) {
  const groupInputRef = useContext(InputGroupInputRefContext);
  const handleRef = (node: HTMLInputElement | null) => {
    if (groupInputRef) {
      groupInputRef.current = node;
    }
    if (typeof ref === "function") {
      ref(node);
    } else if (ref) {
      ref.current = node;
    }
  };
  return (
    <Input
      ref={handleRef}
      data-slot="input-group-control"
      className={cn(
        "flex-1 rounded-none border-0 bg-transparent shadow-none ring-0 ring-offset-0 focus-visible:ring-0 focus-visible:ring-offset-0 focus-visible:outline-none disabled:opacity-100 aria-invalid:ring-0 aria-invalid:outline-none aria-invalid:focus-visible:shadow-none! dark:bg-transparent [&::-webkit-search-cancel-button]:appearance-none",
        className
      )}
      {...props}
    />
  );
}

export { InputGroup, InputGroupAddon, InputGroupButton, InputGroupText, InputGroupInput };
