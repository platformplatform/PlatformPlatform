---
paths: **/*.tsx
description: Rules for translations and internationalization
---

# Translations

Guidelines for implementing translations and internationalization in the frontend, including usage of translation markers and best practices.

## Implementation

1. Use `<Trans>...</Trans>` for TSX/component content
2. Use the t\`...\` syntax for string translations in code
3. Always use plain English text in translation markers
4. Don't hardcode text without proper translation wrappers
5. Use "Sentence case" for everything (buttons, menus, headings, etc.)
6. **Catalogs are split between the shared component library and each self-contained system**:
   - Markers in `application/shared-webapp/ui/**` extract to `application/shared-webapp/ui/translations/locale/{locale}.po` — one shared catalog used by every app
   - Markers in `application/<system>/WebApp/**` extract to that system's `shared/translations/locale/{locale}.po`
   - At runtime, `createFederatedTranslation` merges them: shared underneath, system on top, federated remote (e.g. account loaded into main) on top of that
   - Translate each shared string **once** in the shared catalog, not in every system's catalog
   - Prefer Lingui macros inside `shared-webapp/ui` components over threading translatable text through props -- only accept a text prop when a consumer genuinely needs to override the default (e.g., context-specific wording)
7. Translation workflow:
   - After adding/changing `<Trans>` or t\`\` markers, the `*.po` files are auto-regenerated on build
   - Don't manually add or remove `msgid` entries -- only translate `msgstr` values
   - After auto-generation, translate all new/updated entries for all supported languages
8. **Use correct language-specific characters** in translations -- e.g., Danish requires æøå/ÆØÅ, not ae/oe/aa substitutes. Never approximate with ASCII equivalents
9. Don't translate fully dynamic content such as variable values or dynamic text
9. **Domain terminology consistency**:
   - Use consistent terminology throughout the application
   - Before translating, check existing `*.po` files for established domain terms
   - If "Tenant" is used, always use "Tenant" (not "Customer", "Client", "Organization", etc.)
   - The same English term must translate consistently in each target language
   - Example: "Role" in English should consistently translate to "Rolle" in Danish, not "Funktion" or "Stilling"

## Examples

### Example 1 - Component Translation

```tsx
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Heading } from "@repo/ui/components/Heading";

<Button onClick={handleLogin}>
  <Trans>Log in</Trans>
</Button>

<Heading>
  <Trans>Welcome to {productName}</Trans>
</Heading>
<Trans>Dynamic content: {dynamicContent}</Trans> // ✅ DO: Translate parameterized dynamic content
<Trans>{dynamicContent}</Trans> // ❌ DON'T: Do not translate completely dynamic content
```

### Example 2 - String Translation

```tsx
import { t } from "@lingui/core/macro";

const welcomeMessage = t`Welcome back, ${username}`;
const errorMessage = t`An error occurred while processing your request`;

// Using with functions
alert(t`Are you sure you want to delete this item?`);
```

### Example 3 - Pluralization

```tsx
import { plural } from "@lingui/core/macro";

const message = plural(count, {
  one: "You have # new message",
  other: "You have # new messages",
});
```

