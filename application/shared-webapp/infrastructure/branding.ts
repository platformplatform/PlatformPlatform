// Brand configuration sourced from platform-settings.jsonc, injected at build time via
// import.meta.build_env. Edit platform-settings.jsonc to rebrand every consumer of this module.
export const productName = import.meta.build_env.branding.productName;
// PWA toolbar tint per theme mode. ThemeColorUpdater swaps the `<meta name="theme-color">` value
// at runtime to match the resolved theme; the static HTML default is `themeColor.light`.
export const themeColor = import.meta.build_env.branding.themeColor;
// Locale-keyed web taglines. Components pick the active locale via useLingui().i18n.locale.
// The mail variant lives on the backend (Settings.Current.Branding.Tagline.Mail).
export const webTaglines = import.meta.build_env.branding.tagline.web;
export const contactEmail = import.meta.build_env.branding.contactEmail;
export const supportEmail = import.meta.build_env.branding.supportEmail;
export const showAddToHomescreen = import.meta.build_env.branding.showAddToHomescreen;
export const socialLinks = import.meta.build_env.socialLinks;
