import { t } from "@lingui/core/macro";
import heroDesktopBlurImage from "@/shared/images/hero-desktop-blur.webp";
import heroDesktopImage from "@/shared/images/hero-desktop-xl.webp";

export function HeroImage() {
  return (
    <div
      className="h-auto w-full max-w-[64rem] bg-center bg-cover bg-no-repeat"
      style={{ backgroundImage: `url(${heroDesktopBlurImage})`, aspectRatio: "1000/760" }}
    >
      <img
        src={heroDesktopImage}
        width={1000}
        height={760}
        alt={t`Screenshots of the dashboard project with desktop and mobile versions`}
        fetchPriority="high"
        className="h-auto w-full"
      />
    </div>
  );
}
