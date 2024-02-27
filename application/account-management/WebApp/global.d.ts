export declare global {
  /**
   * Build Environment Variables
   */
  interface BuildEnv {}

  /**
   * Runtime Environment Variables
   */
  interface RuntimeEnv {
    /* Public url / base url */
    PUBLIC_URL: string;
    /* CDN url / location of client bundle files */
    CDN_URL: string;
    /* Application version */
    APPLICATION_VERSION: string;
    /* Culture locale */
    LOCALE: string;
  }

  interface UserInfoEnv {
    /* Is user authenticated */
    isAuthenticated: boolean;
    /* User locale */
    locale: string;
    /* User email */
    email?: string;
    /* User name */
    name?: string;
    /* User role */
    role?: string;
    /* Tenant id */
    tenantId?: string;
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
