import { t } from "@lingui/core/macro";
import { Image } from "@repo/ui/components/Image";
import heroMobileImage from "@/shared/images/hero-mobile-xl.webp";
import heroMobileBlurImage from "@/shared/images/hero-mobile-blur.webp";
import heroDesktopImage from "@/shared//images/hero-desktop-xl.webp";
import heroDesktopBlurImage from "@/shared//images/hero-desktop-blur.webp";

export function HeroImage() {
  return (
    <>
      <Image
        src={heroMobileImage}
        blurDataURL={heroMobileBlurImage}
        width={560}
        height={620}
        className="block md:hidden"
        alt={t`Screenshots of the dashboard project with mobile versions`}
        priority
      />
      <Image
        src={heroDesktopImage}
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
