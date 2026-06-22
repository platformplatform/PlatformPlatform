import { Tabs as TabsPrimitive } from "@base-ui/react/tabs";
import { cva, type VariantProps } from "class-variance-authority";
import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";

import { cn } from "../utils";

function Tabs({ className, ...props }: TabsPrimitive.Root.Props) {
  return <TabsPrimitive.Root data-slot="tabs" className={cn("flex flex-col gap-4", className)} {...props} />;
}

function TabsList({ className, ...props }: TabsPrimitive.List.Props) {
  const listRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

  const updateScrollState = useCallback(() => {
    const el = listRef.current;
    if (!el) return;
    setCanScrollLeft(el.scrollLeft > 1);
    setCanScrollRight(el.scrollLeft < el.scrollWidth - el.clientWidth - 1);
  }, []);

  useEffect(() => {
    const el = listRef.current;
    if (!el) return;
    updateScrollState();
    const observer = new ResizeObserver(updateScrollState);
    observer.observe(el);
    return () => observer.disconnect();
  }, [updateScrollState]);

  const scroll = (direction: "left" | "right") => {
    const el = listRef.current;
    if (!el) return;
    el.scrollBy({ left: direction === "left" ? -120 : 120, behavior: "smooth" });
  };

  return (
    <div data-slot="tabs-list-wrapper" className="relative -m-1">
      <TabsPrimitive.List
        ref={listRef}
        data-slot="tabs-list"
        className={cn(
          "relative flex [scrollbar-width:none] gap-1 overflow-x-auto border-b border-border p-1 [&::-webkit-scrollbar]:hidden",
          className
        )}
        onScroll={updateScrollState}
        {...props}
      />
      {canScrollLeft && (
        <button
          type="button"
          aria-hidden
          tabIndex={-1}
          onClick={() => scroll("left")}
          className="absolute top-1 bottom-1 left-0 z-10 flex w-12 cursor-pointer items-center justify-start bg-gradient-to-r from-background from-50% to-transparent"
        >
          <ChevronLeftIcon className="size-4 text-muted-foreground" />
        </button>
      )}
      {canScrollRight && (
        <button
          type="button"
          aria-hidden
          tabIndex={-1}
          onClick={() => scroll("right")}
          className="absolute top-1 right-0 bottom-1 z-10 flex w-12 cursor-pointer items-center justify-end bg-gradient-to-l from-background from-50% to-transparent"
        >
          <ChevronRightIcon className="size-4 text-muted-foreground" />
        </button>
      )}
    </div>
  );
}

const tabTriggerVariants = cva(
  "relative inline-flex cursor-pointer scroll-mx-12 items-center justify-center gap-2 rounded-md px-4 py-2 text-sm font-semibold whitespace-nowrap outline-ring transition-colors not-data-[active]:text-muted-foreground after:absolute after:right-1 after:-bottom-1 after:left-1 after:h-0.5 after:transition-colors not-data-[active]:after:bg-transparent not-data-[active]:hover:text-muted-foreground/90 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:bg-muted/50 disabled:pointer-events-none disabled:opacity-50 data-[active]:text-foreground data-[active]:after:bg-primary",
  {
    variants: {
      size: {
        default: "h-[var(--control-height)]",
        sm: "h-[var(--control-height-sm)] text-xs",
        lg: "h-[var(--control-height-lg)]"
      }
    },
    defaultVariants: {
      size: "default"
    }
  }
);

function TabsTrigger({
  className,
  size = "default",
  ...props
}: TabsPrimitive.Tab.Props & VariantProps<typeof tabTriggerVariants>) {
  const triggerRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const el = triggerRef.current;
    if (!el) return;
    const observer = new MutationObserver(() => {
      if (el.hasAttribute("data-active")) {
        el.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "nearest" });
      }
    });
    observer.observe(el, { attributes: true, attributeFilter: ["data-active"] });
    return () => observer.disconnect();
  }, []);

  return (
    <TabsPrimitive.Tab
      ref={triggerRef}
      data-slot="tabs-trigger"
      className={cn(tabTriggerVariants({ size, className }))}
      {...props}
    />
  );
}

function TabsContent({ className, ...props }: TabsPrimitive.Panel.Props) {
  return (
    <TabsPrimitive.Panel
      data-slot="tabs-content"
      className={cn(
        "rounded-md outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2",
        className
      )}
      {...props}
    />
  );
}

export { Tabs, TabsList, TabsTrigger, TabsContent, tabTriggerVariants };
