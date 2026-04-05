import { t } from "@lingui/core/macro";

const ROLLOUT_BUCKET_MAX = 100;

export function formatRolloutBucketRange(
  rolloutBucketStart: number,
  rolloutBucketEnd: number,
  rolloutPercentage: number
): string {
  if (rolloutBucketStart <= rolloutBucketEnd) {
    return t`Rollout buckets: ${rolloutBucketStart}-${rolloutBucketEnd} (${rolloutPercentage}%)`;
  }
  return t`Rollout buckets: ${rolloutBucketStart}-99 and 0-${rolloutBucketEnd} (${rolloutPercentage}%)`;
}

export interface RolloutBucketRange {
  bucketStart: number;
  bucketEnd: number;
}

function rolloutBucketSortOrder(
  rolloutBucket: number,
  group: "enabled" | "disabled",
  range: RolloutBucketRange
): number {
  const ref = group === "disabled" ? range.bucketEnd : range.bucketStart;
  return (rolloutBucket - ref + ROLLOUT_BUCKET_MAX) % ROLLOUT_BUCKET_MAX;
}

export function sortBySourceThenRolloutBucket<T>(
  items: T[],
  getSource: (item: T) => string,
  getRolloutBucket: (item: T) => number,
  group: "enabled" | "disabled",
  rolloutBucketRange: RolloutBucketRange | null
): T[] {
  return [...items].sort((a, b) => {
    const aOrder =
      getSource(a) === "manual_override"
        ? -1
        : rolloutBucketRange
          ? rolloutBucketSortOrder(getRolloutBucket(a), group, rolloutBucketRange)
          : 0;
    const bOrder =
      getSource(b) === "manual_override"
        ? -1
        : rolloutBucketRange
          ? rolloutBucketSortOrder(getRolloutBucket(b), group, rolloutBucketRange)
          : 0;
    return aOrder - bOrder;
  });
}
