import type { Appearance } from "@stripe/stripe-js";

function resolveColor(cssVariable: string): string {
  const raw = getComputedStyle(document.documentElement).getPropertyValue(cssVariable).trim();
  return cssToRgb(raw);
}

function cssToRgb(value: string): string {
  const canvas = document.createElement("canvas");
  canvas.width = 1;
  canvas.height = 1;
  const context = canvas.getContext("2d");
  if (!context) {
    return value;
  }
  context.fillStyle = value;
  context.fillRect(0, 0, 1, 1);
  const [r, g, b, a] = context.getImageData(0, 0, 1, 1).data;
  return a < 255 ? `rgba(${r}, ${g}, ${b}, ${(a / 255).toFixed(2)})` : `rgb(${r}, ${g}, ${b})`;
}

function compositeColor(baseCssVariable: string, overlayCssVariable: string, opacity: number): string {
  const canvas = document.createElement("canvas");
  canvas.width = 1;
  canvas.height = 1;
  const context = canvas.getContext("2d");
  if (!context) {
    return resolveColor(baseCssVariable);
  }
  const styles = getComputedStyle(document.documentElement);
  context.fillStyle = styles.getPropertyValue(baseCssVariable).trim();
  context.fillRect(0, 0, 1, 1);
  context.globalAlpha = opacity;
  context.fillStyle = styles.getPropertyValue(overlayCssVariable).trim();
  context.fillRect(0, 0, 1, 1);
  const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
  return `rgb(${r}, ${g}, ${b})`;
}

export function getStripeAppearance(): Appearance {
  const isDark = document.documentElement.classList.contains("dark");
  const styles = getComputedStyle(document.documentElement);

  const background = resolveColor("--background");

  return {
    theme: isDark ? "night" : "stripe",
    variables: {
      fontFamily: styles.fontFamily,
      fontSizeBase: styles.fontSize,
      borderRadius: styles.getPropertyValue("--radius").trim(),
      colorPrimary: resolveColor("--primary"),
      colorBackground: background,
      colorText: resolveColor("--card-foreground"),
      colorTextSecondary: resolveColor("--muted-foreground"),
      colorTextPlaceholder: resolveColor("--muted-foreground"),
      colorDanger: resolveColor("--destructive"),
      colorSuccess: resolveColor("--success"),
      colorWarning: resolveColor("--warning"),
      spacingUnit: "0.25rem",
      gridRowSpacing: "0.75rem"
    },
    rules: {
      ".Input": {
        border: `1px solid ${resolveColor("--input")}`,
        boxShadow:
          "var(--tw-inset-shadow), var(--tw-inset-ring-shadow), var(--tw-ring-offset-shadow), var(--tw-ring-shadow), var(--tw-shadow)",
        backgroundColor: isDark ? compositeColor("--background", "--input", 0.3) : resolveColor("--input-background"),
        margin: "0.8rem 0 0.5rem 0"
      },
      ".Tab": {
        border: `1px solid ${resolveColor("--input")}`,
        boxShadow:
          "var(--tw-inset-shadow), var(--tw-inset-ring-shadow), var(--tw-ring-offset-shadow), var(--tw-ring-shadow), var(--tw-shadow)"
      },
      ".Tab--selected": {
        border: `1px solid ${resolveColor("--input")}`,
        boxShadow:
          "var(--tw-inset-shadow), var(--tw-inset-ring-shadow), var(--tw-ring-offset-shadow), var(--tw-ring-shadow), var(--tw-shadow)"
      },
      ".Block": {
        border: "none",
        boxShadow: "none",
        backgroundColor: "transparent",
        padding: "0",
        margin: "0"
      },
      ".AccordionItem": {
        border: "none",
        boxShadow: "none"
      },
      ".Accordion": {
        border: "none",
        boxShadow: "none"
      }
    }
  };
}
