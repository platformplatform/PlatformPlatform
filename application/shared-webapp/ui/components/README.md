# UI Components

This directory contains all shared UI components used across the application.

## Component Inventory

| Component | Source | ShadCN Divergence | Notes |
|-----------|--------|-------------------|-------|
| AddToHomescreen | Custom | - | PWA install prompt for iOS |
| AlertDialog | `npx shadcn@latest add alert-dialog` | Stock ShadCN | - |
| AppLayout | Custom | - | Application layout orchestration |
| Avatar | `npx shadcn@latest add avatar` | Stock ShadCN | - |
| Badge | `npx shadcn@latest add badge` | Stock ShadCN | - |
| Breadcrumb | `npx shadcn@latest add breadcrumb` | text-sm default on BreadcrumbLink | - |
| Button | `npx shadcn@latest add button` | Solid destructive bg for accessibility | - |
| Calendar | `npx shadcn@latest add calendar` | Stock ShadCN (3rd party) | Third-party component using react-day-picker library |
| Checkbox | `npx shadcn@latest add checkbox` | Stock ShadCN | - |
| DateRangePicker | `npx shadcn@latest add calendar-23` | Auto-close on selection, clear button, Field wrapper, custom DateRangeValue interface | ShadCN block template with popover date range picker |
| Dialog | `npx shadcn@latest add dialog` | DirtyDialog integration, mobile full-screen, DialogBody padding and flex layout, DialogTitle top margin | - |
| DirtyDialog | Custom | - | Unsaved changes warning wrapper |
| DropdownMenu | `npx shadcn@latest add dropdown-menu` | w-auto, py-2.5 touch targets | - |
| Field | `npx shadcn@latest add field` | Stock ShadCN | - |
| Form | Custom | - | Validation context provider |
| Input | `npx shadcn@latest add input` | Stock ShadCN | - |
| InputGroup | `npx shadcn@latest add input-group` | Stock ShadCN | - |
| InputOtp | `npx shadcn@latest add input-otp` | Stock ShadCN (3rd party) | Third-party component using input-otp library |
| Label | `npx shadcn@latest add label` | Stock ShadCN | - |
| LabelWithTooltip | Custom | - | Label + Tooltip composition |
| Link | Custom | - | TanStack Router integration |
| MarkdownRenderer | Custom | - | Markdown to HTML converter |
| Pagination | `npx shadcn@latest add pagination` | Stock ShadCN | - |
| Popover | `npx shadcn@latest add popover` | Stock ShadCN | - |
| RadioGroup | `npx shadcn@latest add radio-group` | Stock ShadCN | - |
| SearchField | Custom | - | Search input with icons and clear |
| Select | `npx shadcn@latest add select` | alignItemWithTrigger=false, py-3 touch targets | - |
| Separator | `npx shadcn@latest add separator` | Stock ShadCN | - |
| SideMenu | Custom | - | Complex sidebar navigation |
| Sonner | `npx shadcn@latest add sonner` | Stock ShadCN (3rd party) | Third-party toast notification library |
| Table | `npx shadcn@latest add table` | Stock ShadCN | - |
| TablePagination | Custom | - | Pagination wrapper for tables |
| TenantLogo | Custom | - | Avatar wrapper for tenant logos with square shape support |
| TextField | Custom | - | Field + Input + validation composition |
| Toggle | `npx shadcn@latest add toggle` | Stock ShadCN | - |
| Tooltip | `npx shadcn@latest add tooltip` | Stock ShadCN | - |
| UnsavedChangesAlertDialog | Custom | - | Unsaved changes confirmation |

---

## Divergence Documentation

Components marked with divergences intentionally differ from stock ShadCN. Divergences are documented in source code comments using this format:

```typescript
// NOTE: This diverges from stock ShadCN to [reason].
```

When reinstalling a diverged component, reapply the documented changes after installation.
