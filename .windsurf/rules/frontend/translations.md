---
trigger: glob
globs: *.tsx
description: Rules for translations and internationalization
---

# Translations

Carefully follow these instructions when implementing translations and internationalization in the frontend, including usage of translation markers and best practices.

## Implementation

1. Use `<Trans>...</Trans>` for TSX/component content.
2. Use the t\`...\` syntax for string translations in code.
3. Always use plain English text in translation markers.
4. Never hardcode text without proper translation wrappers.
5. Use "Sentence case" for everything (Buttons, Menus, Headings, etc.).
6. Translation workflow:
   - Translation files are located in `shared/translations/locale/` (e.g., `da-DK.po`, `en-US.po`).
   - After adding/changing `<Trans>` or t\`\` markers, the `*.po` files are auto-generated/updated by the build system.
   - Never manually add or remove entries to `*.po` files.
   - **Critical**: After auto-generation updates the `*.po` files, you must translate all new/updated entries in the `*.po` files for all supported languages.
   - Only translate the `msgstr` values; never change `msgid` values.
7. Be careful not to translate fully dynamic content, such as variable values or dynamic text.
8. **Domain terminology consistency**:
   - Use consistent terminology throughout the application.
   - Before translating, check existing `*.po` files to understand established domain terms.
   - If "Tenant" is used, always use "Tenant" (not "Customer", "Client", "Organization", etc.).
   - The same English term must always translate to the same term in each target language.
   - Example: "Role" in English should consistently translate to "Rolle" in Danish (da-DK.po), not "Funktion", "Stilling", etc.

## Examples

### Example 1 - Component Translation

```tsx
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Heading } from "@repo/ui/components/Heading";

<Button onPress={handleLogin}>
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

