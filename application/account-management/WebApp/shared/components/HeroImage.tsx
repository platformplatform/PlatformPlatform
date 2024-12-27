import { t } from "@lingui/core/macro";
import { Image } from "@repo/ui/components/Image";
import heroMobileBlurImage from "@/public/images/hero-mobile-blur.webp";
import heroDesktopBlurImage from "@/public/images/hero-desktop-blur.webp";

export function HeroImage() {
  return (
    <>
      <Image
        src="/images/hero-mobile-xl.webp"
        blurDataURL={heroMobileBlurImage}
        width={560}
        height={620}
        className="block md:hidden"
        alt={t`Screenshots of the dashboard project with mobile versions`}
        priority
      />
      <Image
        src="/images/hero-desktop-xl.webp"
        blurDataURL={heroDesktopBlurImage}
        width={1000}
        height={760}
        className="hidden md:block"
        alt={t`Screenshots of the dashboard project with desktop and mobile versions`}
        priority
      />
    </>
  );
}
