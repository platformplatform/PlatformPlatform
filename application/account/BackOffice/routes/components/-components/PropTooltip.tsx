import type { ReactNode } from "react";

export function PropList({
  title,
  description,
  children
}: {
  title: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-2">
      <div>
        <div className="text-sm font-semibold">{title}</div>
        {description && <div className="text-xs opacity-70">{description}</div>}
      </div>
      <ul className="list-none space-y-1 text-xs">{children}</ul>
    </div>
  );
}

export function Prop({ name, value, children }: { name: string; value?: string; children?: ReactNode }) {
  return (
    <li className="flex gap-1.5">
      <code className="shrink-0 rounded bg-white/10 px-1 py-0.5 font-mono text-[0.6875rem]">
        {value ? `${name}={${value}}` : name}
      </code>
      {children && <span className="opacity-70">{children}</span>}
    </li>
  );
}

export function PropNote({ children }: { children: ReactNode }) {
  return <div className="border-t border-white/10 pt-1.5 text-xs leading-relaxed opacity-70">{children}</div>;
}
