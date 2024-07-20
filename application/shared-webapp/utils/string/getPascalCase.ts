export function getPascalCase(text: string | undefined | null): string {
  if (!text) return "";
  if (text.length === 0) return text;
  return text[0].toUpperCase() + text.slice(1);
}
