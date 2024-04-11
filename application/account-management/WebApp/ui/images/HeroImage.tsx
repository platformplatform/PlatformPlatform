"use client";
import { useLingui } from "@lingui/react";
import Image from "next/image";
import heroMobileImage from "./hero-mobile-xl.webp";
import heroDesktopImage from "./hero-desktop-xl.webp";

export function HeroImage() {
  const { i18n } = useLingui();

  return (
    <>
      <Image
        src={heroMobileImage}
        className="block md:hidden"
        alt={i18n.t("Screenshots of the dashboard project showing mobile versions")}
        priority
      />
      <Image
        src={heroDesktopImage}
        className="hidden md:block"
        alt={i18n.t("Screenshots of the dashboard project showing desktop and mobile versions")}
        priority
      />
    </>
  );
}
