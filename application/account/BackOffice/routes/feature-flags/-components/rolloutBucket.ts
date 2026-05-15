import { t } from "@lingui/core/macro";

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
