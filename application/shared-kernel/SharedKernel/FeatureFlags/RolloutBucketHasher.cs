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

            return (int)(hash % 100 + 1);
        }
    }
}
