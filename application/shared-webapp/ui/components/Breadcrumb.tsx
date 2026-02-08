import { mergeProps } from "@base-ui/react/merge-props";
import { useRender } from "@base-ui/react/use-render";
import { ChevronRightIcon, MoreHorizontalIcon } from "lucide-react";
import type * as React from "react";
import { Children, isValidElement, useEffect, useLayoutEffect, useRef, useState } from "react";
import { cn } from "../utils";

function Breadcrumb({ className, ...props }: React.ComponentProps<"nav">) {
  return <nav aria-label="breadcrumb" data-slot="breadcrumb" className={cn(className)} {...props} />;
}

function BreadcrumbList({ className, children, ...props }: React.ComponentProps<"ol">) {
  const listRef = useRef<HTMLOListElement>(null);

  const items = Children.toArray(children)
    .filter(isValidElement)
    .map((child) => (child.type === BreadcrumbItem ? child : <BreadcrumbItem key={child.key}>{child}</BreadcrumbItem>));

  const itemCount = items.length;
  const hasMiddleItems = itemCount > 2;
  const maxLevel = hasMiddleItems ? 2 : itemCount > 1 ? 1 : 0;
  const [collapseLevel, setCollapseLevel] = useState(0);
  const levelWidthsRef = useRef<number[]>([]);
  const collapseLevelRef = useRef(0);

  useLayoutEffect(() => {
    const list = listRef.current;
    if (!list || maxLevel === 0) {
      return;
    }

    levelWidthsRef.current[collapseLevel] = list.scrollWidth;
    collapseLevelRef.current = collapseLevel;

    if (list.scrollWidth > list.clientWidth && collapseLevel < maxLevel) {
      setCollapseLevel(collapseLevel + 1);
    }
  }, [collapseLevel, maxLevel]);

  useEffect(() => {
    const list = listRef.current;
    if (!list || maxLevel === 0) {
      return;
    }

    const observer = new ResizeObserver(() => {
      const level = collapseLevelRef.current;

      if (level > 0) {
        const prevWidth = levelWidthsRef.current[level - 1];
        if (prevWidth && list.clientWidth >= prevWidth) {
          collapseLevelRef.current = level - 1;
          setCollapseLevel(level - 1);
          return;
        }
      }

      if (list.scrollWidth > list.clientWidth && level < maxLevel) {
        levelWidthsRef.current[level] = list.scrollWidth;
        collapseLevelRef.current = level + 1;
        setCollapseLevel(level + 1);
      }
    });
    observer.observe(list);
    return () => observer.disconnect();
  }, [maxLevel]);

  const showFirstItem = collapseLevel < maxLevel;
  const showMiddleItems = collapseLevel === 0 && hasMiddleItems;
  const showEllipsis = collapseLevel > 0 && itemCount > 1;

  const rendered: React.ReactNode[] = [];

  if (items.length <= 1) {
    rendered.push(...items);
  } else {
    if (showFirstItem) {
      rendered.push(items[0]);
      rendered.push(<BreadcrumbSeparator key="__sep-0" />);
    }

    if (showMiddleItems) {
      for (let i = 1; i < items.length - 1; i++) {
        rendered.push(items[i]);
        rendered.push(<BreadcrumbSeparator key={`__sep-${i}`} />);
      }
    }

    if (showEllipsis) {
      rendered.push(
        <BreadcrumbItem key="__ellipsis">
          <BreadcrumbEllipsis />
        </BreadcrumbItem>
      );
      rendered.push(<BreadcrumbSeparator key="__sep-ellipsis" />);
    }

    rendered.push(items[items.length - 1]);
  }

  return (
    <ol
      ref={listRef}
      data-slot="breadcrumb-list"
      className={cn(
        "-m-1 flex flex-nowrap items-center gap-1.5 overflow-hidden whitespace-nowrap p-1 text-muted-foreground text-sm sm:gap-2.5",
        className
      )}
      {...props}
    >
      {rendered}
    </ol>
  );
}

function BreadcrumbItem({ className, ...props }: React.ComponentProps<"li">) {
  return (
    <li data-slot="breadcrumb-item" className={cn("inline-flex shrink-0 items-center gap-1.5", className)} {...props} />
  );
}

// NOTE: This diverges from stock ShadCN to include text-sm styling.
// This avoids needing size="sm" on every Link rendered inside BreadcrumbLink.
function BreadcrumbLink({ className, render, ...props }: useRender.ComponentProps<"a">) {
  return useRender({
    defaultTagName: "a",
    props: mergeProps<"a">(
      {
        className: cn("text-sm transition-colors hover:text-foreground", className)
      },
      props
    ),
    render,
    state: {
      slot: "breadcrumb-link"
    }
  });
}

function BreadcrumbPage({ className, ...props }: React.ComponentProps<"span">) {
  return (
    <span
      data-slot="breadcrumb-page"
      aria-current="page"
      className={cn("font-normal text-foreground", className)}
      {...props}
    />
  );
}

function BreadcrumbSeparator({ children, className, ...props }: React.ComponentProps<"li">) {
  return (
    <li
      data-slot="breadcrumb-separator"
      role="presentation"
      aria-hidden="true"
      className={cn("shrink-0 [&>svg]:size-3.5", className)}
      {...props}
    >
      {children ?? <ChevronRightIcon />}
    </li>
  );
}

function BreadcrumbEllipsis({ className, ...props }: React.ComponentProps<"span">) {
  return (
    <span
      data-slot="breadcrumb-ellipsis"
      role="presentation"
      aria-hidden="true"
      className={cn("flex size-5 items-center justify-center [&>svg]:size-4", className)}
      {...props}
    >
      <MoreHorizontalIcon />
      <span className="sr-only">More</span>
    </span>
  );
}

export {
  Breadcrumb,
  BreadcrumbList,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbPage,
  BreadcrumbSeparator,
  BreadcrumbEllipsis
};
