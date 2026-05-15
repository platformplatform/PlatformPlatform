namespace SharedKernel.FeatureFlags;

public static class RolloutBucketHasher
{
    // Total number of buckets in the rollout space. A 1% rollout covers exactly one bucket; a 100%
    // rollout covers BucketCount buckets (indices 0..MaxBucketInclusive).
    public const int BucketCount = 100;

    public const int MaxBucketInclusive = BucketCount - 1;

    // Effective bucket for an AB.NeverOn pin: the bucket immediately before BucketStart, wrapping around
    // the [0, BucketCount) range. This is the bucket that gets included only when rollout reaches 100%,
    // i.e., the last one in the rollout sequence for this flag.
    public static int ComputeNeverOnBucket(int bucketStart)
    {
        return (bucketStart - 1 + BucketCount) % BucketCount;
    }

    public static int ComputeRolloutBucket(int sequenceNumber)
    {
        var value = VanDerCorput(sequenceNumber);
        return (int)(value * BucketCount);
    }

    public static bool IsInRolloutBucketRange(int bucket, int rolloutBucketStart, int rolloutBucketEnd)
    {
        if (rolloutBucketStart <= rolloutBucketEnd)
        {
            return bucket >= rolloutBucketStart && bucket <= rolloutBucketEnd;
        }

        // Wrap-around case (e.g., start=90, end=10 means 90-99 and 0-10)
        return bucket >= rolloutBucketStart || bucket <= rolloutBucketEnd;
    }

    // Stable per-flag starting bucket derived from the flag key. SetFeatureFlagRolloutPercentage uses this
    // to assign a non-zero rollout range, so every flag has a deterministic starting offset regardless of the
    // current rollout state.
    public static int ComputeStartingRolloutBucket(string flagKey)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in flagKey)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            return (int)(hash % BucketCount);
        }
    }

    // The rollout percentage at which the given bucket first becomes included in an A/B rollout for this flag.
    // Returns 1-100. Useful for surfacing "this account/user gets the feature when rollout reaches N%" in admin UIs.
    public static int ComputeInclusionThresholdPercentage(int bucket, string flagKey)
    {
        var start = ComputeStartingRolloutBucket(flagKey);
        return (bucket - start + BucketCount) % BucketCount + 1;
    }

    // How many buckets are covered by a [start, end] rollout range, handling the wrap-around case
    // (e.g., start=45, end=0 covers buckets 45..99 + 0 = 56). Both endpoints are inclusive. Returns
    // null when either endpoint is null (no rollout configured, i.e., 0%).
    public static int? ComputeRolloutPercentage(int? bucketStart, int? bucketEnd)
    {
        if (bucketStart is null || bucketEnd is null) return null;
        return (bucketEnd.Value - bucketStart.Value + BucketCount) % BucketCount + 1;
    }

    private static double VanDerCorput(int n)
    {
        double result = 0;
        double denominator = 2;

        while (n > 0)
        {
            result += (n & 1) / denominator;
            n >>= 1;
            denominator *= 2;
        }

        return result;
    }
}
