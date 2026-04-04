namespace SharedKernel.FeatureFlags;

public static class RolloutBucketHasher
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    public static int ComputeBucket(string entityId)
    {
        unchecked
        {
            var hash = FnvOffsetBasis;
            foreach (var c in entityId)
            {
                hash ^= c;
                hash *= FnvPrime;
            }

            return (int)(hash % 99 + 1);
        }
    }

    public static bool IsInBucketRange(int bucket, int bucketStart, int bucketEnd)
    {
        // Bucket 0 = always opt-in (internal testers), included in any rollout
        if (bucket == 0) return true;

        // Bucket 100 = always opt-out (VIP customers), only included at 100% rollout
        if (bucket == 100) return bucketStart == 0 && bucketEnd == 100;

        if (bucketStart <= bucketEnd)
        {
            return bucket >= bucketStart && bucket <= bucketEnd;
        }

        // Wrap-around case (e.g., start=90, end=10 means 90-99 and 1-10)
        return bucket >= bucketStart || bucket <= bucketEnd;
    }
}
