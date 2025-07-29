import { useEffect, useRef } from "react";

interface UseInfiniteScrollProps {
  enabled: boolean;
  hasMore: boolean;
  isLoadingMore: boolean;
  onLoadMore: () => void;
}

export function useInfiniteScroll({ enabled, hasMore, isLoadingMore, onLoadMore }: UseInfiniteScrollProps) {
  const loadMoreRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!enabled || !loadMoreRef.current || !hasMore || isLoadingMore) {
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) {
          onLoadMore();
        }
      },
      { threshold: 0.5 }
    );

    observer.observe(loadMoreRef.current);
    return () => observer.disconnect();
  }, [enabled, hasMore, isLoadingMore, onLoadMore]);

  return loadMoreRef;
}
