import { ShieldX } from "lucide-react";
import type { ReactNode } from "react";

interface AccessDeniedContentProps {
  title: ReactNode;
  description: ReactNode;
}

export function AccessDeniedContent({ title, description }: AccessDeniedContentProps) {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-8 px-6 py-12 text-center">
      <div className="flex max-w-lg flex-col items-center gap-6">
        <div className="flex h-20 w-20 items-center justify-center rounded-full bg-destructive/10">
          <ShieldX className="h-10 w-10 text-destructive" />
        </div>

        <div className="flex flex-col gap-3">
          <h1 className="font-bold text-3xl text-foreground">{title}</h1>
          <p className="text-lg text-muted-foreground">{description}</p>
        </div>
      </div>
    </div>
  );
}
