export function SidePaneSection({
  label,
  trailing,
  children,
  className
}: Readonly<{ label: string; trailing?: React.ReactNode; children: React.ReactNode; className?: string }>) {
  return (
    <div className={`flex flex-col gap-2 py-4${className ? ` ${className}` : ""}`}>
      <div className="flex h-6 items-center justify-between gap-2">
        <span className="text-xs leading-6 font-semibold tracking-wider text-muted-foreground uppercase">{label}</span>
        {trailing}
      </div>
      {children}
    </div>
  );
}

export function SidePaneDivider() {
  return <div className="-mx-6 border-t border-border" />;
}
