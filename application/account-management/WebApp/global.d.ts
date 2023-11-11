export declare global {
  /**
   * Build Environment Variables
   */
  type BuildEnv = {
    /* Version of client build */
    VERSION: string;
  };

  /**
   * Runtime Environment Variables
   */
  type RuntimeEnv = {
    /* Public url / base url */
    PUBLIC_URL: string;
    /* CDN url / location of client bundle files */
    CDN_URL: string;
  };

  /**
   * Both Build and Runtime Environment variables
   */
  type Environment = BuildEnv & RuntimeEnv;

  declare interface ImportMeta {
    env: Environment;
    build_env: BuildEnv;
    runtime_env: RuntimeEnv;
  }
}
