# UI Components

This directory contains all shared UI components used across the application.

## Component Inventory

| Component | Source | ShadCN Divergence | Notes |
|-----------|--------|-------------------|-------|
| AddToHomescreen | Custom | - | PWA install prompt for iOS |
| AlertDialog | `npx shadcn@latest add alert-dialog` | Stock ShadCN | - |
| AppLayout | Custom | - | Application layout orchestration |
| Avatar | `npx shadcn@latest add avatar` | Stock ShadCN | - |
| Badge | `npx shadcn@latest add badge` | Outline-based focus ring | - |
| Breadcrumb | `npx shadcn@latest add breadcrumb` | text-sm default on BreadcrumbLink | - |
| Button | `npx shadcn@latest add button` | Outline focus ring, solid destructive bg, bg-white on outline/secondary | - |
| Calendar | `npx shadcn@latest add calendar` | Stock ShadCN (3rd party) | Third-party component using react-day-picker library |
| Checkbox | `npx shadcn@latest add checkbox` | Outline focus ring | - |
| DateRangePicker | `npx shadcn@latest add calendar-23` | Auto-close on selection, clear button, Field wrapper, hover:bg-white trigger | ShadCN block template |
| Dialog | `npx shadcn@latest add dialog` | DirtyDialog integration, mobile full-screen, DialogBody padding and flex layout, DialogTitle top margin | - |
| DirtyDialog | Custom | - | Unsaved changes warning wrapper |
| DropdownMenu | `npx shadcn@latest add dropdown-menu` | w-auto, py-2.5 touch targets | - |
| Field | `npx shadcn@latest add field` | Stock ShadCN | - |
| Form | Custom | - | Validation context provider |
| Input | `npx shadcn@latest add input` | Outline focus ring and bg-white | - |
| InputGroup | `npx shadcn@latest add input-group` | Outline focus-within and suppressed inner input focus ring | - |
| InputOtp | `npx shadcn@latest add input-otp` | Stock ShadCN (3rd party) | Third-party component using input-otp library |
| Label | `npx shadcn@latest add label` | Stock ShadCN | - |
| LabelWithTooltip | Custom | - | Label + Tooltip composition |
| Link | Custom | - | TanStack Router integration |
| MarkdownRenderer | Custom | - | Markdown to HTML converter |
| Pagination | `npx shadcn@latest add pagination` | Stock ShadCN | - |
| Popover | `npx shadcn@latest add popover` | Stock ShadCN | - |
| RadioGroup | `npx shadcn@latest add radio-group` | Outline-based focus ring | - |
| SearchField | Custom | - | Search input with icons and clear |
| Select | `npx shadcn@latest add select` | Outline focus ring, alignItemWithTrigger=false, py-3 touch targets, bg-white | - |
| Separator | `npx shadcn@latest add separator` | Stock ShadCN | - |
| SideMenu | Custom | - | Complex sidebar navigation |
| Sonner | `npx shadcn@latest add sonner` | Stock ShadCN (3rd party) | Third-party toast notification library |
| Table | `npx shadcn@latest add table` | Outline focus ring on TableRow | - |
| TablePagination | Custom | - | Pagination wrapper for tables |
| TenantLogo | Custom | - | Avatar wrapper for tenant logos with square shape support |
| TextField | Custom | - | Field + Input + validation composition |
| Toggle | `npx shadcn@latest add toggle` | Outline focus ring | - |
| Tooltip | `npx shadcn@latest add tooltip` | Stock ShadCN | - |
| UnsavedChangesAlertDialog | Custom | - | Unsaved changes confirmation |

---

## Global Divergence Patterns

All ShadCN components have these common modifications applied:

1. **Focus ring**: Ring utilities (`focus-visible:ring-*`) replaced with outline-based approach (`outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2`)
3. **Background colors**: `bg-transparent` replaced with `bg-white dark:bg-input/30` for explicit light-mode backgrounds

## Divergence Documentation

Components with divergences are documented in source code using:

```typescript
// NOTE: This diverges from stock ShadCN to [reason].
```

When reinstalling a component, reapply all documented divergences plus the global patterns above.
