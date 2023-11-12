import { resolve } from "path";
import { Configuration, DefinePlugin } from "@rspack/core";
import HtmlWebpackPlugin from "html-webpack-plugin";
import HtmlWebpackHarddiskPlugin from "html-webpack-harddisk-plugin";

const buildEnv: BuildEnv = {
  VERSION: process.env.BUILD_VERSION ?? "-.-.-",
};

const configuration: Configuration = {
  context: __dirname,
  entry: {
    runtime: "./src/lib/rspack/runtime.ts",
    main: "./src/main.tsx",
  },
  output: {
    filename: process.env.NODE_ENV === "production" ? "[name].[contenthash].bundle.js" : undefined,
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
    // @ts-ignore
    new HtmlWebpackPlugin({
      template: "./public/index.html",
      meta: {
        runtimeEnv: "<ENCODED_RUNTIME_ENV>",
      },
      alwaysWriteToDisk: true,
      publicPath: "<CDN_URL>",
    }),
    new DefinePlugin({
      "import.meta.build_env": JSON.stringify(buildEnv),
      "import.meta.runtime_env": "getPlatformPlatformEnvironment().runtimeEnv",
      "import.meta.env": "getPlatformPlatformEnvironment().env",
    }),
    new HtmlWebpackHarddiskPlugin({
      outputPath: resolve(__dirname, "dist"),
    }),
  ],
};

export default configuration;
