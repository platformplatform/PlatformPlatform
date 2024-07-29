import type { ClientMethod } from "openapi-fetch";
import type { HttpMethod, MediaType } from "openapi-typescript-helpers";

type CacheEntry = {
  promise: Promise<unknown>;
  expireAt: Date;
};

const cacheEntries = new Map<string, CacheEntry>();
const skipCaching: RequestCache[] = ["no-cache", "no-store", "only-if-cached", "reload"];

const cacheExpiration = 30 * 1000;

export function createCachedClientMethod<
  // biome-ignore lint/complexity/noBannedTypes: This is the exact type used in "openapi-typescript"
  Paths extends Record<string, Record<HttpMethod, {}>>,
  Method extends HttpMethod,
  Media extends MediaType = MediaType,
  R = Awaited<ReturnType<ClientMethod<Paths, Method, Media>>>
>(
  method: (...args: Parameters<ClientMethod<Paths, Method, Media>>) => Promise<R>
): (...args: Parameters<ClientMethod<Paths, Method, Media>>) => Promise<R> {
  return (...params: Parameters<ClientMethod<Paths, Method, Media>>): Promise<R> => {
    const key = btoa(JSON.stringify(params));
    const { cache } = params[1] ?? {};

    if (skipCaching.includes(cache ?? "default")) {
      // Skip caching
      cacheEntries.delete(key);
      return method(...params);
    }

    // Prune expired cache entries
    const now = new Date();
    for (const [key, entry] of cacheEntries) {
      if (entry.expireAt < now) {
        cacheEntries.delete(key);
      }
    }

    const entry = cacheEntries.get(key);
    if (entry && cache !== "reload") {
      // Return the cached promise
      return entry.promise as Promise<R>;
    }

    // Create a new cache entry
    const cacheEntry: CacheEntry = {
      promise: method(...params),
      expireAt: new Date(Date.now() + cacheExpiration)
    };
    cacheEntries.set(key, cacheEntry);

    return cacheEntry.promise as Promise<R>;
  };
}
