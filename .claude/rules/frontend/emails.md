---
paths: application/*/WebApp/emails/**,application/shared-webapp/emails/**
description: Authoring localized transactional email templates with React Email plus the @repo/emails component library and Scriban helpers
---

# Emails

Guidelines for authoring transactional email templates that ship as Scriban-substituted HTML and plaintext for the .NET backend's email renderer.

## Implementation

1. Pick the right home for the template:
   - Templates owned by a self-contained system live under `application/<system>/WebApp/emails/templates/<Name>.tsx` (each system has its own `lingui.config.ts`, `translations/locale/{locale}.po`, optional `static/`, and a build-emitted `dist/` folder)
   - Generic showcase templates that demonstrate every shared component live under `application/shared-webapp/emails/showcase/` and ship alongside the package itself
   - Default-export the template component; the build looks up the default export
   - Wrap the body in `<TransactionalEmail locale={...} preview={...}>` from `@repo/emails/components/TransactionalEmail` and include exactly one `<Subject>` element so the renderer can extract the email subject from the rendered HTML

2. Use the `@repo/emails` component library for layout and content:
   - `Heading`, `Subject`, `Button`, `Avatar`, `AvatarGroup`, `Badge`, `Alert`, `ProgressBar`, `Separator`, `Image`, `TransactionalEmail`
   - Add new components only when an existing one is genuinely insufficient — prefer composition
   - Style with Tailwind classes already supported by `@react-email/components`. Email-friendly Tailwind cannot inline complex selectors, so avoid `[&_*]:`, `first:[&_*]:`, and similar child-combinator variants — use inline `style={{ ... }}` for per-child overrides instead

3. Substitute runtime values with the JSX helpers — never hand-write `{{ ... }}` in text content:
   - `<Value path="user.firstName" sample="Alex" />` → emits `{{ user.firstName }}` in the build, renders `Alex` in the dev preview
   - `<Loop path="items" sample={[{...}, {...}]}>{() => <div><Value path="item.name" sample="" /></div>}</Loop>` → emits `{{ for item in items }}...{{ end }}`. The iteration variable inside the loop is always named `item` (this is the Scriban binding name, fixed by the helper — the JSX render callback parameter is unused), so field references inside the loop must use `item.field` (Scriban requires explicit binding for field access; there is no implicit `this` like in Handlebars `{{#each}}`)
   - `<If path="hasBalance" sample={true}>...<Else>...</Else></If>` → emits `{{ if hasBalance }}...{{ else }}...{{ end }}`. Omit `<Else>` to skip the else branch
   - `<OtpAutofill code="otpCode" domain="domain" />` emits the iOS-compatible `@{domain} #{code}` autofill suffix; place it once at the end of the template body so it lands on the last line of the plaintext output
   - **Exception — HTML attributes and Trans strings.** `<Value>` renders a `<span>`, so it cannot live inside attributes like `href` or inside the literal text of a `<Trans>` marker. In those two cases, hand-write the Scriban placeholder verbatim:
     - Attribute: `<Link href="{{LoginUrl}}">…</Link>` (the entire attribute value is a single placeholder)
     - Trans string: `<Trans>{`You have been invited to join '{{'TenantName'}}' on '{{'ProductName'}}'`}</Trans>` — the ICU single-quote escape `'{{'…'}}'` is required so Lingui doesn't mistake `{{TenantName}}` for an ICU placeholder. The same escaped form lands verbatim in the `.po` files

4. Use the Scriban helpers registered in `SharedKernel/Emails/EmailHelpers.cs` for value formatting (called with Scriban pipe syntax):
   - `amount | format_currency "USD" "en-US"` — both currency code and locale are required
   - `value | format_date "en-US" "yyyy-MM-dd"?` — format is optional and defaults to the locale's long date pattern
   - `count | pluralize "singular" "plural"?` — explicit plural is optional; Humanizer derives one when omitted
   - Pass these as the `path` of `<Value>` (e.g., `<Value path={'balance | format_currency "USD" "en-US"'} sample="$129.00" />`). `<Value>` renders raw HTML in build mode so the embedded quotes are preserved verbatim for Scriban

5. Translate every user-facing string with the Lingui macro form, exactly like the rest of the app:
   - Import `Trans` from `@lingui/react/macro` and use the children form: `<Trans>Welcome to PlatformPlatform</Trans>`. Lingui's CLI applies the macro at extract time to populate `.po` ids, and the email build aliases `@lingui/react/macro` (via `tsconfig.json` paths and a tiny runtime wrapper at `application/shared-webapp/emails/build/lingui-macro-runtime.tsx`) so Node-side rendering produces the same id-and-message contract the macro generates in the SPA build
   - For inline JSX inside translated copy, just nest it: `<Trans>Hello <Value path="firstName" sample="Alex" /></Trans>`. The wrapper walks children and emits `<0/>` placeholders matching the macro
   - The build runs `lingui extract --clean` and `lingui compile --typescript` per target before rendering, so re-running the build is enough to regenerate `.po` files after adding new markers
   - Translate every empty `msgstr` in every locale before handoff — never approximate Danish characters (`æøå`) with ASCII

6. Build and preview pipelines:
   - **Standalone email build:** `dotnet run --project developer-cli -- build --emails` — fastest path while iterating; outputs `<Name>.<locale>.html` and `<Name>.<locale>.txt` to `application/<system>/WebApp/emails/dist/` (per-system templates) and `application/shared-webapp/emails/dist/` (showcase templates)
   - **Full frontend build:** `dotnet run --project developer-cli -- build --frontend` (and the no-flag default) include the email build automatically via turbo
   - **Dev preview:** from `application/shared-webapp/emails`, run `npm run dev` to launch the React Email dev server on port 4000 with the showcase folder. Per-system previews can launch their own server by pointing `--dir` at `application/<system>/WebApp/emails/templates`
   - The build sets `EMAIL_RENDER_MODE=build`; the dev preview leaves it unset so the helpers substitute their `sample` props

7. Brand assets:
   - Place static assets (logos, hero images, fonts) in `application/<system>/WebApp/emails/static/`
   - Reference them with absolute URLs that resolve through the backend's `UseEmailStaticFiles()` middleware (e.g., `https://app.dev.localhost/emails/assets/logo.png`)
   - The same markup works in dev and production because both serve `/emails/assets/` from the same dist directory

## Examples

### Example 1 - Minimal Localized Template

```tsx
// application/account/WebApp/emails/templates/Welcome.tsx
import { Trans } from "@lingui/react/macro";
import { Text } from "@react-email/components";

import { Heading } from "@repo/emails/components/Heading";
import { Subject } from "@repo/emails/components/Subject";
import { TransactionalEmail } from "@repo/emails/components/TransactionalEmail";
import { Value } from "@repo/emails/helpers/Value";

export default function Welcome({ locale }: { locale: string }) {
  return (
    <TransactionalEmail locale={locale} preview="Welcome to {{ProductName}}">
      <Subject>
        <Trans>{`Welcome to '{{'ProductName'}}'`}</Trans>
      </Subject>
      <Heading level={1}>
        <Trans>Hi <Value path="user.firstName" sample="Alex" /></Trans>
      </Heading>
      <Text>
        <Trans>Thanks for signing up. Your account is ready.</Trans>
      </Text>
    </TransactionalEmail>
  );
}
```

### Example 2 - Helpers and Conditional Branches

```tsx
// ✅ DO: Use the macro form, prefix loop fields with `item.`, and pipe values through helpers
<Loop path="invoices" sample={[{ id: "1042" }, { id: "1043" }]}>
  {() => (
    <div>
      <Trans>Invoice <Value path="item.id" sample="" /></Trans>
    </div>
  )}
</Loop>

<If path="hasBalance" sample={true}>
  <Trans>
    Outstanding balance:{" "}
    <Value path={'balance | format_currency "USD" "en-US"'} sample="$129.00" />
  </Trans>
  <Else>
    <Trans>You are all caught up.</Trans>
  </Else>
</If>

// ❌ DON'T: Hand-write Scriban in JSX strings or invent opaque ids
<div>{"{{ for invoice in invoices }}{{ invoice.id }}{{ end }}"}</div>
<Trans id="email.invoice.line">Invoice <0/></Trans>

// ❌ DON'T: Reference loop fields without the `item.` prefix — Scriban needs explicit binding
<Loop path="invoices" sample={[{ id: "1042" }]}>
  {() => <div><Value path="id" sample="" /></div>}
</Loop>
```

### Example 3 - OTP Autofill

```tsx
// ✅ DO: Place OtpAutofill at the end of the email body so the iOS suffix is the last line of plaintext
<TransactionalEmail locale={locale} preview="Your verification code">
  <Subject>
    <Trans>Your verification code</Trans>
  </Subject>
  <Heading>
    <Value path="otpCode" sample="ABC123" />
  </Heading>
  <OtpAutofill code="otpCode" domain="domain" />
</TransactionalEmail>
```
