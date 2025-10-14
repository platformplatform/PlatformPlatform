import { t } from "@lingui/core/macro";
import { Image } from "@repo/ui/components/Image";
import heroDesktopBlurImage from "@/shared//images/hero-desktop-blur.webp";
import heroDesktopImage from "@/shared//images/hero-desktop-xl.webp";

export function HeroImage() {
  return (
    <Image
      src={heroDesktopImage}
      blurDataUrl={heroDesktopBlurImage}
      width={1000}
      height={760}
      alt={t`Screenshots of the dashboard project with desktop and mobile versions`}
      priority={true}
    />
  );
}
