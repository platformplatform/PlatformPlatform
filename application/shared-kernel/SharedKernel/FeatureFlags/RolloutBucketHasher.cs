namespace SharedKernel.FeatureFlags;

public static class RolloutBucketHasher
{
    public static int ComputeBucket(int sequenceNumber)
    {
        var value = VanDerCorput(sequenceNumber);
        return (int)(value * 100);
    }

    public static bool IsInBucketRange(int bucket, int bucketStart, int bucketEnd)
    {
        if (bucketStart <= bucketEnd)
        {
            return bucket >= bucketStart && bucket <= bucketEnd;
        }

        // Wrap-around case (e.g., start=90, end=10 means 90-99 and 0-10)
        return bucket >= bucketStart || bucket <= bucketEnd;
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
