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
     * Application ID e.g. "account-management/webapp"
     **/
    APPLICATION_ID: string;
    /**
     * Build type e.g. "development" | "production"
     */
    BUILD_TYPE: "development" | "production";
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
  }

  export interface UserInfoEnv {
    /**
     * Identifier
     */
    id: string;
    /**
     * Is user authenticated
     **/
    isAuthenticated: boolean;
    /**
     * User locale
     **/
    locale: string;
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
     * User role
     **/
    role?: "Owner" | "Admin" | "Member";
    /**
     * Tenant id
     **/
    tenantId?: string;
    /**
     * Avatar url
     **/
    avatarUrl?: string;
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
  }
}
