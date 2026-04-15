# Custom icons

Custom SVG icons that aren't available in [Lucide](https://lucide.dev/icons). Prefer Lucide when a suitable match exists — only add a custom icon when no Lucide icon fits the visual intent.

Each icon is a plain `.tsx` file with an inline `<svg>`. No bundler plugin or build step is required — RSBuild compiles them like any other component.

## Conventions

- **Props**: `forwardRef<SVGSVGElement, LucideProps>` and spread `...rest` onto `<svg>` so icons are drop-in compatible with Lucide.
- **Defaults**: `size={24}`, `strokeWidth={2}`.
- **SVG attrs**: `viewBox="0 0 24 24"`, `fill="none"`, `stroke="currentColor"`, `strokeLinecap="round"`, `strokeLinejoin="round"`.
- **No fills, no hardcoded colors** — `currentColor` lets callers drive the color via Tailwind `text-*` utilities.
- **`displayName`** — set it for devtools and tests.

## Adding a new icon

1. Copy `TeamsIcon.tsx` as a template.
2. Rename the exported component and the file (e.g. `FooIcon.tsx`).
3. Replace the `<path>` / `<circle>` / `<rect>` elements with your SVG content. Keep the wrapper `<svg>` attributes unchanged.
4. Update `displayName`.
5. Import where needed:

```tsx
import { TeamsIcon } from "@repo/ui/icons/TeamsIcon";
```

## Finding SVG inspiration

- [icones.js.org](https://icones.js.org/) — unified search across Material, Tabler, Phosphor, Heroicons, etc. Copy the raw `<path d="..."/>` and adjust to match our stroke style.
- [Lucide icon set](https://lucide.dev/icons) — reference for the visual style we mimic.

When adapting an icon from another set, strip fills, switch to `stroke="currentColor"`, set `strokeLinecap`/`strokeLinejoin` to `round`, and normalize to a 24×24 viewBox.
