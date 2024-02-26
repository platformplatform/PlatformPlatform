import os from "node:os";
import fs from "node:fs";
import { join, resolve } from "node:path";
import process from "node:process";
import type { Configuration } from "@rspack/core";
import { CopyRspackPlugin, DefinePlugin, HtmlRspackPlugin } from "@rspack/core";
import { ClientFilesystemRouterPlugin } from "@platformplatform/client-filesystem-router/rspack-plugin";

const buildEnv: BuildEnv = {};

const outputPath = resolve(__dirname, "dist");

if (fs.existsSync(outputPath))
  fs.rmSync(outputPath, { recursive: true });

const configuration: Configuration = {
  context: __dirname,
  entry: {
    main: ["./shared/rspack/runtime.ts", "./main.tsx"],
  },
  output: {
    clean: true,
    publicPath: "auto",
    path: outputPath,
    filename: process.env.NODE_ENV === "production" ? "[name].[contenthash].bundle.js" : undefined,
  },
  resolve: {
    tsConfigPath: resolve(__dirname, "tsconfig.json"),
  },
  module: {
    rules: [
      {
        /**
         * For now RSPack does not support plugins in the builtin SWC loader.
         * This is a workaround to allow the use of macros for translations - can be removed once the builtin SWC supports plugins.
         * (Note: swc-loader is used instead of swc-loader because swc-loader supports plugins)
         */
        test: /\.tsx?$/,
        exclude: /node_modules/,
        use: {
          loader: "swc-loader",
          options: {
            sourceMap: true,
            jsc: {
              parser: {
                syntax: "typescript",
                tsx: true,
              },
              transform: {
                react: {
                  runtime: "automatic",
                },
              },
              experimental: {
                plugins: [["@lingui/swc-plugin", {}]],
              },
            },
          },
        },
      },
      {
        test: /\.svg$/i,
        type: "asset",
        resourceQuery: /url/, // *.svg?url
      },
      {
        test: /\.svg$/i,
        issuer: /\.tsx?$/,
        resourceQuery: "", // exclude react component if *.svg?url
        use: [
          {
            loader: "@svgr/webpack",
            options: {
              svgoConfig: {
                plugins: [
                  {
                    name: "preset-default",
                    params: {
                      overrides: {
                        removeViewBox: false,
                      },
                    },
                  },
                ],
              },
            },
          },
        ],
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
      {
        test: /\.(png|jpg|ico|webp|webm)$/i,
        type: "asset",
      },
    ],
  },
  plugins: [
    new HtmlRspackPlugin({
      template: "./public/index.html",
      meta: {
        runtimeEnv: "%ENCODED_RUNTIME_ENV%",
      },
      publicPath: "%CDN_URL%",
    }),
    new CopyRspackPlugin({
      patterns: [
        {
          from: "public",
          to: outputPath,
          globOptions: {
            ignore: ["**/index.html"],
          },
        },
      ],
    }),
    new DefinePlugin({
      "import.meta.build_env": JSON.stringify(buildEnv),
      "import.meta.runtime_env": "getApplicationEnvironment().runtimeEnv",
      "import.meta.env": "getApplicationEnvironment().env",
    }),
    new ClientFilesystemRouterPlugin({
      dir: "app",
    }),
  ],
  devServer: {
    allowedHosts: "all",
    headers: {
      "Access-Control-Allow-Origin": "*",
    },
    port: 8444,
    server: {
      type: "https",
      options: {
        pfx: join(os.homedir(), ".aspnet", "https", "localhost.pfx"),
        passphrase: process.env.CERTIFICATE_PASSWORD,
      },
    },
    devMiddleware: {
      writeToDisk: (filename) => {
        // Write files to disk enabling the Api to serve them
        return /index.html$/.test(filename) || /robots.txt$/.test(filename) || /favicon.ico$/.test(filename);
      },
    },
  },
};

export default configuration;
