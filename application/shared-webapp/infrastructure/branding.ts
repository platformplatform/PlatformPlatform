// Brand configuration sourced from platform-settings.jsonc, injected at build time via
// import.meta.build_env. Edit platform-settings.jsonc to rebrand every consumer of this module.
export const productName = import.meta.build_env.branding.productName;
export const contactEmail = import.meta.build_env.branding.contactEmail;
export const supportEmail = import.meta.build_env.branding.supportEmail;
export const socialLinks = import.meta.build_env.socialLinks;
