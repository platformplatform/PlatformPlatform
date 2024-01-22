import { themes } from "@storybook/theming";
import type { Preview } from "@storybook/react";
import "../src/main.css";

const preview: Preview = {
  parameters: {
    actions: { argTypesRegex: "^on[A-Z].*" },
    controls: {
      matchers: {},
    },
    docs: {
      theme: window.matchMedia("(prefers-color-scheme: dark)").matches ? themes.dark : themes.light,
    },
  },
};

export default preview;
