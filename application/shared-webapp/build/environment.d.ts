export declare global {
  /**
   * Custom build environment variables
   */
  interface CustomBuildEnv {}
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
