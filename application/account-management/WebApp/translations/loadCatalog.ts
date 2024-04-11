import { type Messages } from "@lingui/core";

const messageCache = new Map<string, Messages>();

export async function loadCatalog(locale: string) {
  if (messageCache.has(locale) === false) {
    const { messages } = (await import(
      `@lingui/loader!./locale/${locale}.po`
    )) as { messages: Messages };
    messageCache.set(locale, messages);
  }

  return messageCache.get(locale) as Messages;
}
