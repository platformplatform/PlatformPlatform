namespace SharedKernel.FeatureFlags;

public static class RolloutBucketHasher
{
    public static int ComputeRolloutBucket(int sequenceNumber)
    {
        var value = VanDerCorput(sequenceNumber);
        return (int)(value * 100);
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

            return (int)(hash % 100);
        }
    }

    // The rollout percentage at which the given bucket first becomes included in an A/B rollout for this flag.
    // Returns 1-100. Useful for surfacing "this account/user gets the feature when rollout reaches N%" in admin UIs.
    public static int ComputeInclusionThresholdPercentage(int bucket, string flagKey)
    {
        var start = ComputeStartingRolloutBucket(flagKey);
        return (bucket - start + 100) % 100 + 1;
    }

    // How many buckets are covered by a [start, end] rollout range, handling the wrap-around case
    // (e.g., start=45, end=0 covers buckets 45..99 + 0 = 56). Both endpoints are inclusive. Returns
    // null when either endpoint is null (no rollout configured, i.e., 0%).
    public static int? ComputeRolloutPercentage(int? bucketStart, int? bucketEnd)
    {
        if (bucketStart is null || bucketEnd is null) return null;
        return (bucketEnd.Value - bucketStart.Value + 100) % 100 + 1;
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
