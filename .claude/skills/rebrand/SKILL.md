---
name: rebrand
description: Apply a downstream brand to a PlatformPlatform fork. Edits one config file, drops in eight supplied logo assets, renames the solution and CLI to the new brand, and rotates UserSecretsId across every csproj. Skips all other source files. Use once per downstream fork after cloning, or to re-flip a brand later.
allowed-tools: Read, Write, Edit, Bash, AskUserQuestion
---

# Rebrand

Apply a downstream brand to a PlatformPlatform fork. The rebrand is intentionally narrow: brand values live in one JSONC file, logo assets live at canonical paths, and a handful of names (solution file, CLI alias, UserSecretsId, Docker prefix) flip in lockstep. Source code is not touched.

This skill expects the upstream canonical layout — primary logos named `logo-{light,dark}-88.png` and square marks named `logo-mark-{light,dark}-192.png`. If you are on an older PlatformPlatform that still uses `logo-wrap-*` or `-512` mark names, the upstream needs the one-time rename first.

## STEP 1: Verify a clean working tree

```bash
git status --porcelain
```

Stop if there is anything staged or unstaged. Rebrand touches many files at once and a mid-rebrand merge with unrelated changes is hard to untangle.

## STEP 2: Collect inputs

Use AskUserQuestion to collect all values up front. Do not write anything until every value is in hand — otherwise a partial rebrand leaves the fork in a broken state.

Required inputs:

1. **Product name** — proper-cased product label shown in UI, emails, page titles. Example: `Acme`.
2. **CLI alias** — short lowercase shell alias replacing `pp`. Example: `acme`. No spaces, no dashes inside the binary name.
3. **Solution name** — base filename for `PlatformPlatform.slnx`. Example: `Acme` (yielding `Acme.slnx`).
4. **Internal email domain** — `@example.com` (leading `@` required). Classifies users as internal for telemetry segmentation. BackOffice access is controlled by Entra ID, not by this setting.
5. **Taglines** — one-line product descriptions split across two channels and per locale:
   - `tagline.web.<locale>` — shown in the landing page footer (consumer reads the active Lingui locale).
   - `tagline.mail.<locale>` — shown in the email footer (renderer reads the template's locale).
   The two maps must list the SAME locales (backend fails loud at startup if they diverge); the en-US value is REQUIRED. Collect en-US and da-DK for both channels — for downstream brands the web and mail copy will often differ (e.g. the email leans toward the recipient context). For PlatformPlatform itself, web == mail.
6. **Primary color** — four CSS color expressions (oklch / hex / hsl / rgb all valid) under `branding.primaryColor`: `light`, `lightForeground`, `dark`, `darkForeground`. Drives the "button color" and its on-color text in both light and dark modes. The value flows through theme.css via an inline `<style>` block injected per HTML template (`--brand-primary` / `--brand-primary-foreground` CSS variables); `--primary` and `--sidebar-primary` read those. Pick foregrounds with enough contrast against the primary — that is a deliberate brand-owner decision, not auto-derived.
7. **PWA chrome colors** — hex only, no oklch. `branding.themeColor.light` / `branding.themeColor.dark` tint the mobile browser/PWA toolbar (iOS Dynamic Island "tint band"); the frontend swaps the `<meta name="theme-color">` value at runtime to match the resolved theme. `branding.themeColor.light` is the install-time fallback baked into `manifest.json` (PWA spec is single-valued). `branding.backgroundColor` is the PWA splash-screen background while the SPA boots.
8. **Email header background** — single CSS color (hex / rgb) used behind the transparent email banner PNG at the top of every transactional email. Same value in light- and dark-mode email clients, so pick a color that reads well in both contexts. Field: `branding.emailHeaderBackground`.
9. **Contact email** — public-facing email shown as a mail icon in landing page footer. Empty hides it.
10. **Support email** — shown to signed-in users in the in-app support dialog. Required (the dialog renders the value unconditionally; an empty string shows a blank field).
11. **Show Add to Home Screen** — `branding.showAddToHomescreen` boolean. When `true` the user-facing app renders the iOS "Add to Home Screen" install prompt (it self-limits to iOS Safari outside an installed PWA); `false` suppresses it entirely. BackOffice never shows the prompt regardless.
12. **Social links** — six separate fields: GitHub, LinkedIn, YouTube, X, Facebook, Instagram. Each is a full URL or empty string.

Required logo assets (user provides absolute paths to eight files):

| Spec | Canonical path written to |
| --- | --- |
| Primary logo, dark ink on transparent, 88px tall | `application/shared-webapp/ui/images/logo-light-88.png` |
| Primary logo, white ink on transparent, 88px tall | `application/shared-webapp/ui/images/logo-dark-88.png` |
| Square icon mark for light bg, transparent, 192x192 | `application/shared-webapp/ui/images/logo-mark-light-192.png` |
| Square icon mark for dark bg, transparent, 192x192 | `application/shared-webapp/ui/images/logo-mark-dark-192.png` |
| Favicon, multi-res 16/32/48 | `application/main/WebApp/public/favicon.ico` AND `application/account/BackOffice/public/favicon.ico` |
| Apple touch icon, light variant, 180x180, solid background | `application/main/WebApp/public/apple-touch-icon.png` AND `application/account/BackOffice/public/apple-touch-icon.png` |
| Apple touch icon, dark variant, 180x180, solid background (iOS 13+ picks this when the user is in dark mode via the `media="(prefers-color-scheme: dark)"` link in index.html) | `application/main/WebApp/public/apple-touch-icon-dark.png` AND `application/account/BackOffice/public/apple-touch-icon-dark.png` |
| Email banner, 1200x184, transparent PNG, logo centered (the logo occupies a 640x88 area with 280px transparent padding left/right and 48px top/bottom). Renders full-bleed at 600x92 and scales down on mobile; the email header background shows through the transparent areas. | `application/main/WebApp/public/email/logo-1200x184.png` |

Optional hero imagery (single shared pair, rendered side-by-side with the login/signup form on `/login`, `/signup`, and BackOffice login):

| Spec | Canonical path written to |
| --- | --- |
| Hero image, 1000x760 aspect, WebP, full quality (rendered with `fetchPriority="high"`) | `application/shared-webapp/ui/images/hero-desktop-xl.webp` |
| Hero image LQIP placeholder, same aspect, heavily compressed WebP (~1 KB; loaded as `background-image` until the XL paints) | `application/shared-webapp/ui/images/hero-desktop-blur.webp` |

The favicon and apple-touch inputs each fan out to two destination paths. The hero pair is single-source: both the user-facing WebApp and the BackOffice login render the same image — replace it once.

## STEP 3: Edit platform-settings.jsonc

Open `application/platform-settings.jsonc` and replace values in `identity`, `branding`, and `socialLinks` with collected inputs. Keep comments on their own lines (the frontend build strips whole-line `//` comments only).

## STEP 4: Replace logo assets

Copy each of the eight user-supplied files to its canonical path. Filenames never change. Favicon and apple-touch fan out to both `WebApp` and `BackOffice` paths — copy the same source file to both destinations.

The email banner is a plain drop-in: overwrite `logo-1200x184.png` in place. It is a fixed 1200x184 transparent slot rendered by `Header.tsx` — no `.tsx` edit, no filename change.

Verify each destination exists and is the size the user supplied:

```bash
ls -la application/shared-webapp/ui/images/logo-*.png application/main/WebApp/public/favicon.ico application/main/WebApp/public/apple-touch-icon.png application/account/BackOffice/public/favicon.ico application/account/BackOffice/public/apple-touch-icon.png application/main/WebApp/public/email/logo-1200x184.png
```

## STEP 5: Rename the solution

```bash
git mv application/PlatformPlatform.slnx application/<SolutionName>.slnx
git mv application/PlatformPlatform.slnx.DotSettings application/<SolutionName>.slnx.DotSettings
```

Update both solution-filter files to point at the new path:

- `application/main/Main.slnf` — `"path": "..\\<SolutionName>.slnx"`
- `application/account/Account.slnf` — same

## STEP 6: Change the developer CLI alias

Edit `developer-cli/DeveloperCli.csproj`:

```xml
<AssemblyName><cli-alias></AssemblyName>
```

This single rename cascades automatically via `Configuration.AliasName` (which reads `Assembly.GetExecutingAssembly().GetName().Name`). The shell alias, the per-CLI config file (`{alias}.json`), the git-hooks consent file, the change-detection env var (`{ALIAS}_SKIP_CHANGE_DETECTION`), and every user-facing install message all flip without further edits.

## STEP 7: Rotate UserSecretsId across every csproj

Every `.csproj` carrying a `<UserSecretsId>platformplatform-<old-guid></UserSecretsId>` element needs a fresh GUID and the new alias prefix.

Find them:

```bash
grep -rln "<UserSecretsId>platformplatform-" application developer-cli
```

Generate ONE new GUID and use the SAME `<UserSecretsId><cli-alias>-<new-guid></UserSecretsId>` value in every match. All projects must share the identical UserSecretsId — secrets live at `~/.microsoft/usersecrets/<id>/secrets.json` and every project reads from that single store. Different GUIDs would split the store per project and nothing would resolve secrets.

Existing user secret stores on disk (`~/.microsoft/usersecrets/`) are not migrated — developers may need to re-run any local secret setup after the rotation.

## STEP 8: Set the Docker volume prefix

Set `development.dockerVolumePrefix` in `platform-settings.jsonc` to the product name lowercased, with spaces replaced by hyphens (e.g. productName `"Acme Foo Bar"` -> `"acme-foo-bar"`). The Aspire AppHost (which names the volumes) and the developer CLI's `stop` command (which removes them) both read this single value through `DockerVolumeNaming.ResolveVolumePrefix`, so it keeps each fork's volumes isolated on a shared machine. Use the product name, not the CLI alias — the volume identity tracks the brand.

## STEP 9: Update GitHub Actions

Replace solution-file references in every workflow:

```bash
grep -rln "PlatformPlatform\.slnx" .github/workflows
```

Files known to reference it: `code-style.yml`, `app-gateway.yml`. Replace `PlatformPlatform.slnx` with `<SolutionName>.slnx`. Also update the human-readable label in `code-style.yml` (e.g. `full backend (PlatformPlatform.slnx)` -> `full backend (<SolutionName>.slnx)`).

## STEP 10: Leave the bootstrap quickstart image alone

`cloud-infrastructure/cluster/deploy-cluster.sh` and `cloud-infrastructure/modules/container-app.bicep` reference `ghcr.io/platformplatform/quickstart:latest` as a default image. This is an upstream-provided empty hello-world container used during initial Bicep deployment, before the downstream's own container registry exists. Chicken-and-egg: Bicep creates the registry, but the first deployment needs a placeholder image. Downstream forks keep using the upstream image. Do not touch these files.

Illustrative comments in `cloud-infrastructure/` and `.github/workflows/` use `example.com` (RFC 2606 reserved domain) as placeholder sender domains. Nothing to change.

## STEP 11: Audit for hardcoded references

The skill assumes the codebase reaches every brand-coupled value through configuration (`platform-settings.jsonc`, `Configuration.AliasName`, the `dockerVolumePrefix` constant, the new UserSecretsId). If something hardcodes the old brand, that is a centralization gap upstream — surface it, do not silently patch.

Run these greps across the whole repo (excluding build artifacts):

```bash
grep -rln "PlatformPlatform" . \
  --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj --exclude-dir=artifacts \
  --exclude-dir=dist --exclude-dir=.turbo --exclude-dir=.git
grep -rln "platformplatform" . \
  --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj --exclude-dir=artifacts \
  --exclude-dir=dist --exclude-dir=.turbo --exclude-dir=.git
grep -rln "\\bpp\\b" . \
  --include="*.cs" --include="*.csproj" --include="*.yml" --include="*.sh" --include="*.ts" --include="*.tsx"
```

Expected remaining hits after the rebrand:

- `README.md`, `LICENSE`, `CODEOWNERS`, `CONTRIBUTING.md` — out of scope for this skill (the rewrite is project-specific); not a brand-leak.
- `cloud-infrastructure/cluster/deploy-cluster.sh` and `cloud-infrastructure/modules/container-app.bicep` — the bootstrap quickstart image, left intentionally (see step 10).

Anything else is a finding. For each hit:

1. Read the file to understand whether the reference is brand-coupled (must change) or a stable identifier (e.g. an upstream package name in `pull-platformplatform-changes`).
2. If brand-coupled and the value should come from configuration but doesn't, fix the upstream code so future rebrands stop hitting this — do not just patch the literal here. Report the fix as a centralization improvement that should land in PlatformPlatform.
3. If brand-coupled but trivially local (e.g. a comment), update inline.

## STEP 12: Verify

Run in order:

1. `build --quiet` — full backend + frontend build. Any failure here means the rebrand broke something the rebrand itself was supposed to leave untouched. Investigate before continuing.
2. `aspire-restart` — restart the AppHost. Should print the new product name in startup logs.
3. `e2e --smoke --quiet` — confirms the SPA renders, login works, and the new product name appears where the tests assert it.
4. Visual smoke in the browser: open the SPA, confirm the footer logo, the favicon, the page title, and the email-template preview all show the new brand.

Final check:

```bash
git diff --stat
```

The diff should only touch:

- `application/platform-settings.jsonc`
- `application/shared-webapp/ui/images/logo-*.png`
- `application/main/WebApp/public/favicon.ico`
- `application/main/WebApp/public/apple-touch-icon.png`
- `application/main/WebApp/public/apple-touch-icon-dark.png`
- `application/account/BackOffice/public/favicon.ico`
- `application/account/BackOffice/public/apple-touch-icon.png`
- `application/account/BackOffice/public/apple-touch-icon-dark.png`
- `application/main/WebApp/public/email/logo-1200x184.png`
- `application/<SolutionName>.slnx` (renamed from PlatformPlatform.slnx)
- `application/<SolutionName>.slnx.DotSettings` (renamed)
- `application/main/Main.slnf`
- `application/account/Account.slnf`
- `developer-cli/DeveloperCli.csproj`
- Every `*.csproj` with a rotated UserSecretsId
- A small number of `.cs` files that read the UserSecretsId prefix
- `.github/workflows/*.yml`

Source code outside this list should be untouched. If anything else shows up in the diff, stop and investigate before committing.

## Out of scope

The skill does not touch:

- `README.md`, `LICENSE`, `CODEOWNERS`, `CONTRIBUTING.md` — these are not required for the fork to run, and the correct rewrite is project-specific (license terms, README narrative, code-owner team names). Out of scope because the skill does not know how, not because they should stay.
- `package.json` workspace scopes — already brand-neutral (`@repo/*`), nothing to do.
- C# root namespaces — every csproj has its own assembly-scoped namespace; nothing reads `PlatformPlatform.*`. Nothing to do.

A downstream that wants to rewrite README/LICENSE/CODEOWNERS does that in a separate pass.
