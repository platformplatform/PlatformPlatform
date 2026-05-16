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
       * Contact email shown as a mail icon in the landing page footer (empty hides the icon)
       */
      contactEmail: string;
      /**
       * Support email shown to signed-in users in the in-app support dialog
       */
      supportEmail: string;
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
