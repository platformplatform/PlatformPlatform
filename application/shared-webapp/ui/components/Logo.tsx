import logoWordmarkDark from "../images/logo-dark-88.png";
import logoWordmarkLight from "../images/logo-light-88.png";
import logoMarkDark from "../images/logo-mark-dark-192.png";
import logoMarkLight from "../images/logo-mark-light-192.png";
import { useTheme } from "../theme/mode/ThemeMode";

type LogoProps = {
  /** `wordmark` is the full horizontal logo, `mark` is the square icon. */
  readonly variant: "wordmark" | "mark";
  readonly alt: string;
  readonly className?: string;
};

/**
 * Brand logo that resolves the light or dark asset from the active theme.
 * Centralizing the theme swap here keeps a rebrand to dropping the PNGs into
 * `ui/images/` — no consumer ever picks a light or dark variant by hand.
 */
export function Logo({ variant, alt, className }: LogoProps) {
  const { resolvedTheme } = useTheme();
  const isDark = resolvedTheme
    ? resolvedTheme === "dark"
    : typeof document !== "undefined" && document.documentElement.classList.contains("dark");

  const source =
    variant === "mark" ? (isDark ? logoMarkDark : logoMarkLight) : isDark ? logoWordmarkDark : logoWordmarkLight;

  // `object-contain` keeps non-square brand marks from stretching when a consumer constrains
  // both width and height (e.g. `size-10`). Square marks render identically with or without it.
  return <img src={source} alt={alt} className={`object-contain ${className ?? ""}`} />;
}
