# Translations

Guidelines for implementing translations and internationalization in the frontend, including usage of translation markers and best practices.

## Implementation

1. Use `<Trans>...</Trans>` for TSX/component content
2. Use the t\`...\` syntax for string translations in code
3. Always use plain English text in translation markers
4. Don't hardcode text without proper translation wrappers
5. Use "Sentence case" for everything (buttons, menus, headings, etc.)
6. **Never use Lingui macros in `shared-webapp`** -- they only work in application WebApps where the Lingui compiler runs. Shared components should accept translatable text as props
7. Translation workflow:
   - Translation files are in `shared/translations/locale/` (e.g., `da-DK.po`, `en-US.po`)
   - After adding/changing `<Trans>` or t\`\` markers, the `*.po` files are auto-generated/updated by the build system
   - Don't manually add or remove entries to `*.po` files
   - After auto-generation, translate all new/updated entries in `*.po` files for all supported languages
   - Only translate `msgstr` values—never change `msgid` values
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
  <Trans>Welcome to PlatformPlatform</Trans>
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

