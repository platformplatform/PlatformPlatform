export function getCamelCase(text: string | undefined | null): string {
  if (!text) return "";
  if (text.length === 0) return text;
  return text[0].toLowerCase() + text.slice(1);
}
