import type { ReactElement } from "react";

import { i18n } from "@lingui/core";
import { I18nProvider } from "@lingui/react";
import { render } from "@react-email/render";
import { loadPlatformSettings } from "@repo/build/platformSettings";
import { spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { createElement } from "react";

import i18nConfig from "../../infrastructure/translations/i18n.config.json" with { type: "json" };

process.env.EMAIL_RENDER_MODE = "build";

const here = dirname(fileURLToPath(import.meta.url));
const sharedEmailsRoot = resolve(here, "..");
const applicationRoot = resolve(sharedEmailsRoot, "..", "..");

const SYSTEMS = ["account", "main"] as const;
const LOCALES = Object.keys(i18nConfig);

// Sample values used to substitute hand-written Scriban placeholders ({{ Name }}) that cannot go
// through the JSX helpers — typically those that live inside href attributes or Lingui <Trans>
// strings (see emails.md "Exception — HTML attributes and Trans strings"). Applied only to the
// preview render output, never to the production .html/.txt artifacts.
//
// SignupUrl/LoginUrl render as root-relative paths — they are nav links, fine to resolve against
// whatever host serves the preview iframe. ProductName is a brand value (not dummy model data),
// sourced from platform-settings so the preview subject line is brand-accurate.
//
// {{PublicUrl}} is deliberately NOT listed: it stays unsubstituted in the preview artifacts so the
// back-office resolves it at serve time to the public app URL — the always-on host real emails
// load their logo and assets from (see UseEmailStaticFiles).
const PREVIEW_PLACEHOLDER_VALUES: Record<string, string> = {
  SignupUrl: "/signup",
  LoginUrl: "/login",
  TenantName: "Acme Corp",
  ProductName: loadPlatformSettings().branding.productName
};

function substitutePreviewPlaceholders(input: string): string {
  let result = input;
  for (const [name, value] of Object.entries(PREVIEW_PLACEHOLDER_VALUES)) {
    // Scriban accepts both `{{Name}}` and `{{ Name }}` — replace both forms.
    result = result.replaceAll(`{{${name}}}`, value).replaceAll(`{{ ${name} }}`, value);
  }
  return result;
}

type BuildTarget = {
  label: string;
  // Folder containing lingui.config.ts and the translations/ directory.
  configRoot: string;
  // Folder containing the .tsx template files (default-exported components).
  templatesDir: string;
  // Folder where <Name>.<locale>.html and <Name>.<locale>.txt are written.
  distDir: string;
};

async function main(): Promise<void> {
  const targets: BuildTarget[] = [];

  // Per-system templates: only included when the system actually has an emails/templates folder.
  for (const system of SYSTEMS) {
    const systemRoot = join(applicationRoot, system, "WebApp", "emails");
    if (!existsSync(join(systemRoot, "templates"))) continue;
    targets.push({
      label: `${system}/WebApp/emails`,
      configRoot: systemRoot,
      templatesDir: join(systemRoot, "templates"),
      distDir: join(systemRoot, "dist")
    });
  }

  if (targets.length === 0) {
    console.log("No email templates found. Skipping email build.");
    return;
  }

  for (const target of targets) {
    // eslint-disable-next-line no-await-in-loop
    await buildTarget(target);
  }
}

async function buildTarget(target: BuildTarget): Promise<void> {
  console.log(`[emails] Building ${target.label}...`);

  // Reset the dist directory so renames don't leave orphans behind.
  rmSync(target.distDir, { recursive: true, force: true });
  mkdirSync(target.distDir, { recursive: true });

  // Lingui compile reads the local lingui.config.ts and writes <locale>.ts beside the .po files.
  // We invoke it here so the dynamic imports below load fresh catalogs.
  runLingui(target, ["extract", "--clean", "--config", "lingui.config.ts"]);
  runLingui(target, ["compile", "--typescript", "--config", "lingui.config.ts"]);

  const templates = readdirSync(target.templatesDir).filter((file) => file.endsWith(".tsx"));

  for (const file of templates) {
    const templatePath = join(target.templatesDir, file);
    const name = file.replace(/\.tsx$/, "");
    // eslint-disable-next-line no-await-in-loop
    await renderTemplate(target, templatePath, name);
  }
}

function runLingui(target: BuildTarget, args: string[]): void {
  const result = spawnSync("npx", ["lingui", ...args], { cwd: target.configRoot, stdio: "inherit" });
  if (result.status !== 0) {
    throw new Error(`lingui ${args[0]} failed for ${target.label} with exit code ${result.status}.`);
  }
}

async function renderTemplate(target: BuildTarget, templatePath: string, name: string): Promise<void> {
  // tsx loads the .tsx template at runtime; the file must default-export the JSX template component.
  const moduleUrl = pathToFileURL(templatePath).href;
  const templateModule = (await import(moduleUrl)) as { default: (props: { locale: string }) => ReactElement };
  const Template = templateModule.default;

  for (const locale of LOCALES) {
    const catalogUrl = pathToFileURL(join(target.configRoot, "translations", "locale", `${locale}.ts`)).href;
    // eslint-disable-next-line no-await-in-loop
    const catalogModule = (await import(`${catalogUrl}?cache-bust=${Date.now()}`)) as {
      messages: Record<string, unknown>;
    };
    i18n.loadAndActivate({ locale, messages: catalogModule.messages });

    const wrapped = createElement(I18nProvider, { i18n }, createElement(Template, { locale }));

    // Two render passes per template/locale:
    //   "build"   → helpers emit Scriban placeholders ({{ Var }}). The backend renders these at
    //              runtime against the real model and sends the final email.
    //   "preview" → helpers substitute their `sample` props with realistic dummy values. The in-app
    //              preview page (BackOffice → Components → Emails) iframes these files so designers
    //              can visually inspect the templates without sending live emails.
    // Both outputs share the same .po catalogs, so translations only need to be entered once.
    for (const mode of ["build", "preview"] as const) {
      process.env.EMAIL_RENDER_MODE = mode;

      // eslint-disable-next-line no-await-in-loop
      const html = await render(wrapped, { pretty: false });
      // eslint-disable-next-line no-await-in-loop
      const text = await render(wrapped, {
        plainText: true,
        // The default html-to-text formatter uppercases <h1>/<h2> content; that mangles Scriban
        // expressions like {{ firstName }} into {{ FIRSTNAME }}, breaking runtime substitution. Pin
        // every heading level to its original casing so the .txt output preserves the template variables.
        htmlToTextOptions: {
          selectors: [
            { selector: "h1", options: { uppercase: false } },
            { selector: "h2", options: { uppercase: false } },
            { selector: "h3", options: { uppercase: false } },
            { selector: "h4", options: { uppercase: false } }
          ]
        }
      });

      const suffix = mode === "build" ? "" : ".preview";
      const finalHtml = mode === "preview" ? substitutePreviewPlaceholders(html) : html;
      const finalText = mode === "preview" ? substitutePreviewPlaceholders(text) : text;
      writeFileSync(join(target.distDir, `${name}.${locale}${suffix}.html`), finalHtml, "utf8");
      writeFileSync(join(target.distDir, `${name}.${locale}${suffix}.txt`), finalText, "utf8");

      console.log(`[emails]   wrote ${name}.${locale}${suffix}.{html,txt}`);
    }
  }
}

try {
  await main();
} catch (error) {
  console.error("[emails] Build failed:", error);
  process.exit(1);
}
