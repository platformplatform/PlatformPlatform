import { useLingui } from "@lingui/react";
import { Image } from "@/ui/components/Image";
import heroMobileBlurImage from "@/public/images/hero-mobile-blur.webp";
import heroDesktopBlurImage from "@/public/images/hero-desktop-blur.webp";

export function HeroImage() {
  const { i18n } = useLingui();

  return (
    <>
      <Image
        src="/images/hero-mobile-xl.webp"
        blurDataURL={heroMobileBlurImage}
        width={560}
        height={620}
        className="block md:hidden"
        alt={i18n.t("Screenshots of the dashboard project showing mobile versions")}
        priority
      />
      <Image
        src="/images/hero-desktop-xl.webp"
        blurDataURL={heroDesktopBlurImage}
        width={1000}
        height={760}
        className="hidden md:block"
        alt={i18n.t("Screenshots of the dashboard project showing desktop and mobile versions")}
        priority
      />
    </>
  );
}
