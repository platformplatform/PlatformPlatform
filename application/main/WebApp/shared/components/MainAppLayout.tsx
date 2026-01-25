import { useSideMenuLayout } from "@repo/ui/hooks/useSideMenuLayout";
import { cn } from "@repo/ui/utils";
import type React from "react";
import { useRef } from "react";

type MainAppLayoutProps = {
  children: React.ReactNode;
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
};

export function MainAppLayout({ children, title, subtitle }: Readonly<MainAppLayoutProps>) {
  const { className, style } = useSideMenuLayout();
  const contentRef = useRef<HTMLDivElement>(null);

  return (
    <div className="flex h-full flex-col">
      <div className={cn(className, "flex h-full flex-col overflow-hidden")} style={style}>
        <main
          ref={contentRef}
          className="flex min-h-0 w-full flex-1 flex-col overflow-y-auto bg-background p-8 pb-[calc(1rem+env(safe-area-inset-bottom,0px))] transition-all duration-100 ease-in-out [-webkit-overflow-scrolling:touch] focus:outline-none"
          id="main-content"
          aria-label="Main content"
          tabIndex={-1}
        >
          <div className="flex h-full flex-col pb-4">
            {title && (
              <div className="mb-4">
                <h1>{title}</h1>
                {subtitle && <p className="mt-2 text-muted-foreground">{subtitle}</p>}
              </div>
            )}
            <div className="flex min-h-0 flex-1 flex-col">{children}</div>
          </div>
        </main>
      </div>
    </div>
  );
}
