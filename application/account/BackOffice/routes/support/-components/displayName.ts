export function getInitials(name: string | null | undefined): string {
  if (!name) return "?";
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
  return initials || "?";
}

export function formatReporterName(firstName?: string | null, lastName?: string | null, email?: string | null): string {
  const composed = `${firstName ?? ""} ${lastName ?? ""}`.trim();
  return composed || email || "Unknown";
}
