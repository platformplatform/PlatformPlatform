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
6. Never change the `*.po` files directly; they are auto-generated based on the uses of `<Trans>...</Trans>` and t\`...\`.
7. Be carefull not to translate fully dynamic content, such as variable values or dynamic text.

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
<Trans>Dynamic content: {dynamicContent}</Trans> // ✅ DO:: Translate parameterized dynamic content
<Trans>{dynamicContent}</Trans> // ❌ DON'T: Do not translate completly dynamic content
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
