import { Suspense, useEffect, useRef } from "react";

type BannerPortalProps = {
  children: React.ReactNode;
};

function useBannerOffset(bannerRef: React.RefObject<HTMLDivElement | null>) {
  useEffect(() => {
    const element = bannerRef.current;
    if (!element) {
      return;
    }

    const updateOffset = () => {
      const height = element.offsetHeight;
      document.documentElement.style.setProperty("--banner-offset", `${height}px`);
    };

    updateOffset();

    const resizeObserver = new ResizeObserver(updateOffset);
    resizeObserver.observe(element);

    return () => {
      resizeObserver.disconnect();
      document.documentElement.style.setProperty("--banner-offset", "0rem");
    };
  }, [bannerRef]);
}

/**
 * Portal target for banners.
 * Place this at the top of your app's root component.
 * Banners will portal their content into this element.
 *
 * The element is fixed at the top of the viewport.
 * z-40 ensures banners stay above mobile sticky header (z-30) during animations.
 * Measures the banner height and sets --banner-offset CSS variable for content positioning.
 */
export function BannerPortal({ children }: BannerPortalProps) {
  const bannerRef = useRef<HTMLDivElement>(null);
  useBannerOffset(bannerRef);

  return (
    <>
      <div ref={bannerRef} id="banner-root" className="fixed top-0 right-0 left-0 z-40" />
      <Suspense>{children}</Suspense>
    </>
  );
}
