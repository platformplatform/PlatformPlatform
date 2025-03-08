import heroDesktopBlurImage from "@/shared//images/hero-desktop-blur.webp";
import heroDesktopImage from "@/shared//images/hero-desktop-xl.webp";
import heroMobileBlurImage from "@/shared/images/hero-mobile-blur.webp";
import heroMobileImage from "@/shared/images/hero-mobile-xl.webp";
import { t } from "@lingui/core/macro";
import { Image } from "@repo/ui/components/Image";

export function HeroImage() {
  return (
    <>
      <Image
        src={heroMobileImage}
        blurDataUrl={heroMobileBlurImage}
        width={560}
        height={620}
        className="block md:hidden"
        alt={t`Screenshots of the dashboard project with mobile versions`}
        priority={true}
      />
      <Image
        src={heroDesktopImage}
        blurDataUrl={heroDesktopBlurImage}
        width={1000}
        height={760}
        className="hidden md:block"
        alt={t`Screenshots of the dashboard project with desktop and mobile versions`}
        priority={true}
      />
    </>
  );
}
