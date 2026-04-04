import { t } from "@lingui/core/macro";

const FNV_OFFSET_BASIS = 2166136261;
const FNV_PRIME = 16777619;
const BUCKET_MAX = 100;

export function computeBucket(entityId: string): number {
  let hash = FNV_OFFSET_BASIS;
  for (let i = 0; i < entityId.length; i++) {
    hash ^= entityId.charCodeAt(i);
    hash = Math.imul(hash, FNV_PRIME) >>> 0;
  }
  return (hash % 99) + 1;
}

export function formatBucketRange(bucketStart: number, bucketEnd: number, rolloutPercentage: number): string {
  if (bucketStart <= bucketEnd) {
    return t`Rollout buckets: ${bucketStart}-${bucketEnd} (${rolloutPercentage}%)`;
  }
  return t`Rollout buckets: ${bucketStart}-100 and 1-${bucketEnd} (${rolloutPercentage}%)`;
}

export interface BucketRange {
  bucketStart: number;
  bucketEnd: number;
}

function bucketSortOrder(entityId: string, group: "enabled" | "disabled", range: BucketRange): number {
  const bucket = computeBucket(entityId);
  const ref = group === "disabled" ? range.bucketEnd : range.bucketStart;
  return (bucket - ref + BUCKET_MAX) % BUCKET_MAX;
}

export function sortBySourceThenBucket<T>(
  items: T[],
  getSource: (item: T) => string,
  getEntityId: (item: T) => string,
  group: "enabled" | "disabled",
  bucketRange: BucketRange | null
): T[] {
  return [...items].sort((a, b) => {
    const aOrder =
      getSource(a) === "manual_override" ? -1 : bucketRange ? bucketSortOrder(getEntityId(a), group, bucketRange) : 0;
    const bOrder =
      getSource(b) === "manual_override" ? -1 : bucketRange ? bucketSortOrder(getEntityId(b), group, bucketRange) : 0;
    return aOrder - bOrder;
  });
}
