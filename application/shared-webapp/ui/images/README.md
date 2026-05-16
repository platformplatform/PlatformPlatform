# Brand logos

This folder is the single source of truth for the brand logos consumed across all SPAs:

- `logo-mark.svg` — square mark used for icon-sized rendering (favicon-style placements, mobile, error pages)
- `logo-wrap.svg` — full wordmark used for headers, login/signup pages, and footers

All SPAs import these via `@repo/ui/images/logo-mark.svg` and `@repo/ui/images/logo-wrap.svg`. To rebrand, replace these two files; nothing else needs to change.

## Related brand assets not centralized here

A few brand assets live elsewhere because they are served as static files by a specific SPA and the path is referenced by external clients (mail-render pipelines, OS install prompts):

- `application/main/WebApp/public/favicon.ico` and `apple-touch-icon.png` (landing-page SPA)
- `application/account/BackOffice/public/favicon.ico` and `apple-touch-icon.png` (back-office SPA)
- `application/main/WebApp/public/email/logo-640x88.png` (rendered into every transactional email by the Scriban template)

Replace those files in place when rebranding.
