import { Skeleton } from "@repo/ui/components/Skeleton";

export function SkeletonPreview() {
  return (
    <div className="flex flex-col gap-2">
      <div className="grid grid-cols-1 gap-6 sm:grid-cols-3">
        <div className="flex flex-col gap-3 rounded-lg border p-4">
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-(--control-height) w-full" />
        </div>
        <div className="flex flex-col gap-3 rounded-lg border p-4">
          <div className="flex items-center gap-3">
            <Skeleton className="size-10 rounded-full" />
            <div className="flex flex-1 flex-col gap-2">
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-3 w-1/2" />
            </div>
          </div>
          <Skeleton className="h-20 w-full" />
        </div>
        <div className="flex flex-col gap-3 rounded-lg border p-4">
          <Skeleton className="h-4 w-1/3" />
          <Skeleton className="h-(--control-height) w-full" />
          <Skeleton className="h-(--control-height) w-full" />
          <Skeleton className="h-(--control-height) w-2/3" />
        </div>
      </div>
    </div>
  );
}
