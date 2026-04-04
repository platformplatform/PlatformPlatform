import { t } from "@lingui/core/macro";

const BUCKET_MAX = 100;

export function formatBucketRange(bucketStart: number, bucketEnd: number, rolloutPercentage: number): string {
  if (bucketStart <= bucketEnd) {
    return t`Rollout buckets: ${bucketStart}-${bucketEnd} (${rolloutPercentage}%)`;
  }
  return t`Rollout buckets: ${bucketStart}-99 and 0-${bucketEnd} (${rolloutPercentage}%)`;
}

export interface BucketRange {
  bucketStart: number;
  bucketEnd: number;
}

function bucketSortOrder(bucket: number, group: "enabled" | "disabled", range: BucketRange): number {
  const ref = group === "disabled" ? range.bucketEnd : range.bucketStart;
  return (bucket - ref + BUCKET_MAX) % BUCKET_MAX;
}

export function sortBySourceThenBucket<T>(
  items: T[],
  getSource: (item: T) => string,
  getBucket: (item: T) => number,
  group: "enabled" | "disabled",
  bucketRange: BucketRange | null
): T[] {
  return [...items].sort((a, b) => {
    const aOrder =
      getSource(a) === "manual_override" ? -1 : bucketRange ? bucketSortOrder(getBucket(a), group, bucketRange) : 0;
    const bOrder =
      getSource(b) === "manual_override" ? -1 : bucketRange ? bucketSortOrder(getBucket(b), group, bucketRange) : 0;
    return aOrder - bOrder;
  });
}
