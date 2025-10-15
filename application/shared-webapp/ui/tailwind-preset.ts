import path from "node:path";
import type { Config } from "tailwindcss";

export default {
  content: [
    "./**/*.{ts,tsx}",
    "./public/index.html",
    path.join(path.dirname(require.resolve("@repo/ui")), "components", "*.js"),
    path.join(path.dirname(require.resolve("@repo/ui")), "components", "**/*.js")
  ],
  darkMode: "selector",
  theme: {
    container: {
      center: true,
      padding: "2rem",
      screens: {
        "2xl": "1400px"
      }
    },
    extend: {
      keyframes: {
        "accordion-down": {
          from: { height: "0" },
          to: { height: "var(--radix-accordion-content-height)" }
        },
        "accordion-up": {
          from: { height: "var(--radix-accordion-content-height)" },
          to: { height: "0" }
        }
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out"
      }
    }
  },
  plugins: [
    require("tailwindcss-react-aria-components"),
    require("tailwindcss-animate"),
    require("@tailwindcss/container-queries"),
    function ({ addUtilities }: { addUtilities: (utilities: Record<string, Record<string, string>>) => void }) {
      addUtilities({
        ".w-dialog-md": {
          "width": "28rem"
        },
        ".w-dialog-lg": {
          "width": "36rem"
        },
        ".w-dialog-xl": {
          "width": "44rem"
        },
        ".w-dialog-2xl": {
          "width": "52rem"
        }
      });
    }
  ]
} satisfies Config;
