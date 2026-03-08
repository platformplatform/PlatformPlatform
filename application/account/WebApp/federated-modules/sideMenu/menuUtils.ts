export const menuItemBaseClassName =
  "flex h-[var(--control-height)] w-full items-center justify-start gap-4 rounded-md px-3 py-2 text-sm hover:bg-hover-background hover:text-foreground active:bg-hover-background";

export function menuItemClassName(pathname: string, itemPath: string, matchPrefix = false) {
  const isActive = matchPrefix ? pathname.startsWith(itemPath) : pathname === itemPath;
  return `${menuItemBaseClassName} ${isActive ? "font-semibold text-foreground" : "font-normal text-muted-foreground"}`;
}
