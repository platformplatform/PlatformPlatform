import { t } from "@lingui/core/macro";

const ROLLOUT_BUCKET_MAX = 100;
const ALWAYS_INCLUDED_BUCKET = 100;

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

function computeSortOrder(
  source: string,
  rolloutBucket: number | null,
  group: "enabled" | "disabled",
  rolloutBucketRange: RolloutBucketRange | null
): number {
  if (source === "ManualOverride") return -1;
  if (rolloutBucket === ALWAYS_INCLUDED_BUCKET) return -0.5;
  if (rolloutBucket === null) return ROLLOUT_BUCKET_MAX;
  return rolloutBucketRange ? rolloutBucketSortOrder(rolloutBucket, group, rolloutBucketRange) : 0;
}

export function sortBySourceThenRolloutBucket<T>(
  items: T[],
  getSource: (item: T) => string,
  getRolloutBucket: (item: T) => number | null,
  group: "enabled" | "disabled",
  rolloutBucketRange: RolloutBucketRange | null
): T[] {
  return [...items].sort((a, b) => {
    const aOrder = computeSortOrder(getSource(a), getRolloutBucket(a), group, rolloutBucketRange);
    const bOrder = computeSortOrder(getSource(b), getRolloutBucket(b), group, rolloutBucketRange);
    return aOrder - bOrder;
  });
}
