namespace SharedKernel.Domain;

public static class RolloutBucketHasher
{
    /// <summary>
    ///     Bucket value 100 means the entity is always included in any rollout, regardless of the rollout range.
    /// </summary>
    public const int AlwaysIncludedBucket = 100;

    public static int ComputeRolloutBucket(int sequenceNumber)
    {
        var value = VanDerCorput(sequenceNumber);
        return (int)(value * 100);
    }

    public static bool IsInRolloutBucketRange(int? bucket, int rolloutBucketStart, int rolloutBucketEnd)
    {
        if (bucket is null) return false;
        if (bucket == AlwaysIncludedBucket) return true;

        if (rolloutBucketStart <= rolloutBucketEnd)
        {
            return bucket >= rolloutBucketStart && bucket <= rolloutBucketEnd;
        }

        // Wrap-around case (e.g., start=90, end=10 means 90-99 and 0-10)
        return bucket >= rolloutBucketStart || bucket <= rolloutBucketEnd;
    }

    /// <summary>
    ///     Computes the Van der Corput sequence value for a given index using base 2 (bit-reversal).
    ///     This low-discrepancy sequence ensures that rollout buckets are evenly distributed
    ///     across the 0-99 range at any population size, avoiding the clustering problem of hash-based approaches.
    /// </summary>
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
