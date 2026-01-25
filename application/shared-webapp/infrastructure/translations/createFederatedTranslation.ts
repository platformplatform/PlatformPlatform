import type { Messages } from "@lingui/core";
import { type Locale, type LocaleFile, Translation } from "./Translation";

// Module federation container type
type FederatedContainer = {
  get(module: string): Promise<() => { messages: Messages }>;
};

// Cache for loaded translation modules
const translationModuleCache = new Map<string, Messages>();

/**
 * Configuration for federated translations
 * Each application that consumes federated modules should configure
 * which remotes might provide translations
 */
const FEDERATED_TRANSLATION_REMOTES = [
  "account"
  // Add more remotes here as they are created
] as const;

/**
 * Creates a Translation instance that automatically loads and merges translations
 * from all federated modules configured in the current application.
 *
 * This function:
 * 1. Uses the base translation loader for the host application
 * 2. Automatically discovers and loads translations from configured federated remotes
 * 3. Merges all translations together with remote translations taking precedence
 *
 * @param baseLoader - Function to load base translations for the host application
 * @returns Translation instance with federated translation support
 */
export function createFederatedTranslation(baseLoader: (locale: Locale) => Promise<LocaleFile>): Promise<Translation> {
  const federatedLoader = createFederatedLoader(baseLoader);
  return Translation.create(federatedLoader);
}

/**
 * Try to load translations from a federated module
 */
async function loadRemoteTranslations(remoteName: string, locale: Locale): Promise<Messages | null> {
  // Check cache first
  const cacheKey = `${remoteName}:${locale}`;
  const cached = translationModuleCache.get(cacheKey);
  if (cached) {
    return cached;
  }

  // Get container using RSBuild's naming convention (hyphens to underscores)
  const containerName = remoteName.replace(/-/g, "_");
  const container = (window as unknown as Record<string, unknown>)[containerName] as FederatedContainer | null;

  if (!container?.get) {
    return null;
  }

  try {
    const factory = await container.get(`./translations/${locale}`);
    const module = factory();

    if (module?.messages) {
      translationModuleCache.set(cacheKey, module.messages);
      return module.messages;
    }
  } catch (_error) {
    // Silently fail - the remote might not have translations for this locale
  }

  return null;
}

/**
 * Creates a translation loader that merges translations from federated modules
 */
function createFederatedLoader(
  baseLoader: (locale: Locale) => Promise<LocaleFile>
): (locale: Locale) => Promise<LocaleFile> {
  return async (locale: Locale): Promise<LocaleFile> => {
    // Load base translations first
    const baseMessages = await baseLoader(locale);

    // Load and merge translations from all configured remotes
    const allMessages = { ...baseMessages.messages };

    await Promise.all(
      FEDERATED_TRANSLATION_REMOTES.map(async (remoteName) => {
        const remoteMessages = await loadRemoteTranslations(remoteName, locale);
        if (remoteMessages) {
          Object.assign(allMessages, remoteMessages);
        }
      })
    );

    return { messages: allMessages };
  };
}
