export declare global {
  /**
   * Custom build environment variables
   */
  interface CustomBuildEnv {
    /**
     * Brand configuration injected from platform-settings.jsonc at build time
     */
    branding: {
      /**
       * Product/platform name displayed throughout the application
       */
      productName: string;
      /**
       * PWA toolbar tint per theme mode. The HTML <meta name="theme-color"> initial value is
       * themeColor.light; ThemeColorUpdater swaps it to themeColor.dark at runtime when dark mode
       * resolves. manifest.json theme_color uses light as the install-time value (PWA spec is
       * single-valued). Hex only.
       */
      themeColor: {
        light: string;
        dark: string;
      };
      /** PWA splash-screen background while the SPA boots (hex, no oklch) */
      backgroundColor: string;
      /**
       * Brand primary color (the "button color"). Light/dark + their foreground (text-on-primary)
       * companions. Piped into theme.css via inline `<style>` in the HTML template.
       */
      primaryColor: {
        light: string;
        lightForeground: string;
        dark: string;
        darkForeground: string;
      };
      /**
       * One-line product description per channel and locale. The frontend reads
       * tagline.web[locale]; emails read tagline.mail[locale] server-side. Locale keys in web and
       * mail must match (the backend fails loud at startup if they diverge).
       */
      tagline: {
        web: Record<string, string>;
        mail: Record<string, string>;
      };
      /**
       * Contact email shown as a mail icon in the landing page footer (empty hides the icon)
       */
      contactEmail: string;
      /**
       * Support email shown to signed-in users in the in-app support dialog
       */
      supportEmail: string;
      /**
       * Whether the iOS "Add to Home Screen" install prompt renders in the user-facing app
       */
      showAddToHomescreen: boolean;
    };
    /**
     * Public social media / community profile links injected from platform-settings.jsonc.
     * Each field is a URL or an empty string; the footer hides any icon whose URL is empty.
     */
    socialLinks: {
      gitHub: string;
      linkedIn: string;
      youTube: string;
      x: string;
      facebook: string;
      instagram: string;
    };
  }
  /**
   * Build Environment Variables
   */
  interface BuildEnv extends CustomBuildEnv {
    /**
     * Application ID e.g. "account/webapp"
     **/
    applicationId: string;
    /**
     * Build type e.g. "development" | "production"
     */
    buildType: "development" | "production";
  }

  /**
   * Runtime Environment Variables
   */
  interface RuntimeEnv {
    /**
     * Public url / base url
     **/
    PUBLIC_URL: string;
    /**
     * CDN url / location of client bundle files
     **/
    CDN_URL: string;
    /**
     * Application version
     **/
    APPLICATION_VERSION: string;
    /**
     * Culture locale
     **/
    LOCALE: string;
    /**
     * Google OAuth enabled
     **/
    PUBLIC_GOOGLE_OAUTH_ENABLED: string;
    /**
     * Whether subscription/billing is enabled (Stripe configured)
     **/
    PUBLIC_SUBSCRIPTION_ENABLED: string;
    /**
     * Whether the in-app support system (ticket inbox + back-office support tabs) is enabled. When
     * disabled, the legacy "Contact support" mailto dialog is shown from the user menu instead.
     **/
    PUBLIC_SUPPORT_SYSTEM_ENABLED?: string;
  }

  export interface UserInfoEnv {
    /**
     * User is authenticated
     **/
    isAuthenticated: boolean;
    /**
     * User locale
     **/
    locale: string;
    /**
     * User Id
     */
    id?: string;
    /**
     * Tenant Id
     **/
    tenantId?: string;
    /**
     * User role
     **/
    role?: "Owner" | "Admin" | "Member";
    /**
     * User email
     **/
    email?: string;
    /**
     * First name
     **/
    firstName?: string;
    /**
     * Last name
     **/
    lastName?: string;
    /**
     * Job title
     */
    title?: string;
    /**
     * Avatar url
     **/
    avatarUrl?: string | null;
    /**
     * Tenant name
     **/
    tenantName?: string;
    /**
     * Tenant logo URL
     **/
    tenantLogoUrl?: string | null;
    /**
     * Is internal user (has access to BackOffice)
     **/
    isInternalUser?: boolean;
    /**
     * Tenant rollout bucket (1-100) for A/B testing
     **/
    tenantRolloutBucket?: number;
    /**
     * User rollout bucket (1-100) for A/B testing
     **/
    userRolloutBucket?: number | null;
    /**
     * Enabled feature flag keys (database-scoped flags evaluated server-side at token issuance)
     **/
    featureFlags?: string[];
  }

  /**
   * Both Build and Runtime Environment variables
   */
  type Environment = BuildEnv & RuntimeEnv;

  declare interface ImportMeta {
    env: Environment;
    build_env: BuildEnv;
    runtime_env: RuntimeEnv;
    user_info_env: UserInfoEnv;
    antiforgeryToken: string;
  }
}
