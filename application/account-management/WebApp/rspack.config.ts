import { resolve } from "path";
import { Configuration, DefinePlugin, HtmlRspackPlugin } from "@rspack/core";

const buildEnv: BuildEnv = {
  VERSION: process.env.BUILD_VERSION ?? "-.-.-",
};

const configuration: Configuration = {
  context: __dirname,
  entry: {
    runtime: "./src/lib/rspack/runtime.ts",
    main: "./src/main.tsx",
  },
  resolve: {
    tsConfigPath: resolve(__dirname, "tsconfig.json"),
  },
  module: {
    rules: [
      {
        test: /\.svg$/,
        type: "asset",
      },
      {
        test: /\.css$/,
        use: [
          {
            loader: "postcss-loader",
            options: {
              postcssOptions: {
                plugins: {
                  tailwindcss: {},
                  autoprefixer: {},
                },
              },
            },
          },
        ],
        type: "css",
      },
    ],
  },
  plugins: [
    new HtmlRspackPlugin({
      template: "./public/index.html",
      meta: {
        // Note(raix): For now we hardcode the runtime environment until the server part is done
        runtimeEnv: btoa(JSON.stringify({ PUBLIC_URL: "https://localhost:8443", CDN_URL: "https://localhost:8080" })),
      },
    }),
    new DefinePlugin({
      "import.meta.build_env": JSON.stringify(buildEnv),
      "import.meta.runtime_env": "getPlatformPlatformEnvironment().runtimeEnv",
      "import.meta.env": "getPlatformPlatformEnvironment().env",
    }),
  ],
};

export default configuration;
