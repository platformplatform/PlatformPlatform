import path from "node:path";

export default ({ config }) => {
  config.resolve.alias = {
    "react": require.resolve("react"),
    "react-dom": require.resolve("react-dom"),
    ...config.resolve.alias,
  };
  config.resolve.tsConfigPath = path.resolve(__dirname, "..", "tsconfig.json");

  config.module.rules = [
    ...config.module.rules,
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
  ];
  return config;
};
