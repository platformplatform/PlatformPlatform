const REGIONAL_INDICATOR_OFFSET = 0x1f1e6 - "A".charCodeAt(0);

export function getCountryFlagEmoji(countryCode: string | null | undefined): string {
  if (!countryCode || countryCode.length !== 2) {
    return "";
  }
  const upper = countryCode.toUpperCase();
  if (!/^[A-Z]{2}$/.test(upper)) {
    return "";
  }
  return String.fromCodePoint(
    upper.charCodeAt(0) + REGIONAL_INDICATOR_OFFSET,
    upper.charCodeAt(1) + REGIONAL_INDICATOR_OFFSET
  );
}

export function getCountryName(countryCode: string | null | undefined, locale: string): string {
  if (!countryCode || countryCode.length !== 2) {
    return "";
  }
  const upper = countryCode.toUpperCase();
  if (!/^[A-Z]{2}$/.test(upper)) {
    return "";
  }
  return new Intl.DisplayNames([locale], { type: "region" }).of(upper) ?? upper;
}
