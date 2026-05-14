import { Skeleton } from "@repo/ui/components/Skeleton";

export function FeatureFlagDetailSkeleton() {
  return (
    <div className="flex flex-col gap-8">
      <Skeleton className="h-20 w-full rounded-lg" />
      <div className="flex flex-col gap-4">
        <Skeleton className="h-6 w-40" />
        <Skeleton className="h-10 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
      </div>
    </div>
  );
}
