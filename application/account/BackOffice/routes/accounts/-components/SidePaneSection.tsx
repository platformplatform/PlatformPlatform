export function SidePaneSection({ label, children }: Readonly<{ label: string; children: React.ReactNode }>) {
  return (
    <div className="flex flex-col gap-2 py-4">
      <span className="text-xs font-semibold tracking-wider text-muted-foreground uppercase">{label}</span>
      {children}
    </div>
  );
}

export function SidePaneDivider() {
  return <div className="-mx-6 border-t border-border" />;
}
