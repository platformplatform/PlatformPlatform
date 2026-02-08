# UI Components

This directory contains all shared UI components used across the application.

## Component Inventory

| Component | Source | ShadCN Divergence | Notes |
|-----------|--------|-------------------|-------|
| AddToHomescreen | Custom | - | PWA install prompt for iOS |
| Alert | `npx shadcn@latest add alert` | warning and info variants | - |
| AlertDialog | `npx shadcn@latest add alert-dialog` | Stock ShadCN | - |
| AppLayout | Custom | - | Application layout orchestration |
| Avatar | `npx shadcn@latest add avatar` | Stock ShadCN | - |
| Badge | `npx shadcn@latest add badge` | Outline-based focus ring | - |
| Breadcrumb | `npx shadcn@latest add breadcrumb` | text-sm default on BreadcrumbLink | - |
| Button | `npx shadcn@latest add button` | Outline focus ring, solid destructive bg, CSS variable heights, bg-white on outline/secondary, cursor-pointer, active backgrounds | - |
| Calendar | `npx shadcn@latest add calendar` | 44px cell size for Apple HIG, weekStartsOn=Monday, locale from app context, capitalized month names, hover maintains selected styling | Third-party component using react-day-picker library |
| Card | `npx shadcn@latest add card` | min-w-0 and overflow-hidden on Card, text-lg and mb-1 on CardTitle, active:bg-muted | - |
| Checkbox | `npx shadcn@latest add checkbox` | Larger size (20px), outline focus ring, 44px tap target, cursor-pointer, active:border-primary | - |
| ContextMenu | `npx shadcn@latest add context-menu` | py-3 touch targets, cursor-pointer on items, active:bg-accent | Right-click (desktop) / long-press (mobile) menu |
| DateRangePicker | `npx shadcn@latest add calendar-23` | Auto-close on selection, clear button, Field wrapper, hover:bg-white trigger, locale-aware date formatting | ShadCN block template |
| Dialog | `npx shadcn@latest add dialog` | DirtyDialog integration, mobile full-screen, DialogBody padding and flex layout, DialogTitle top margin | - |
| DirtyDialog | Custom | - | Unsaved changes warning wrapper |
| DropdownMenu | `npx shadcn@latest add dropdown-menu` | w-auto content, py-3 touch targets, cursor-pointer on items, active:bg-accent | - |
| Empty | `npx shadcn@latest add empty` | Stock ShadCN | Empty state with icon, title, and description |
| Field | `npx shadcn@latest add field` | FieldLabel: BaseUI data-checked attr, border-2, bg-card for Choice Card | - |
| Form | Custom | - | Validation context provider |
| Input | `npx shadcn@latest add input` | CSS variable height, outline focus ring, bg-white | - |
| InputGroup | `npx shadcn@latest add input-group` | CSS variable height, outline focus-within, suppressed inner input focus ring | - |
| InputOtp | `npx shadcn@latest add input-otp` | CSS variable size for Apple HIG | Third-party component using input-otp library |
| Label | `npx shadcn@latest add label` | Stock ShadCN | - |
| LabelWithTooltip | Custom | - | Label + Tooltip composition |
| Link | Custom | - | TanStack Router integration |
| MarkdownRenderer | Custom | - | Markdown to HTML converter |
| Pagination | `npx shadcn@latest add pagination` | CSS variable size for Apple HIG | - |
| Popover | `npx shadcn@latest add popover` | Stock ShadCN | - |
| RadioGroup | `npx shadcn@latest add radio-group` | Outline-based focus ring, cursor-pointer, active:border-primary | - |
| ScrollArea | `npx shadcn@latest add scroll-area` | Stock ShadCN | Use inside DialogBody for intentionally scrollable content (lists, collections) |
| Select | `npx shadcn@latest add select` | Outline focus ring, CSS variable heights, alignItemWithTrigger=false, py-3 touch targets, bg-white, cursor-pointer, active:bg-accent | - |
| Separator | `npx shadcn@latest add separator` | Stock ShadCN | - |
| Skeleton | `npx shadcn@latest add skeleton` | bg-muted instead of bg-accent | Loading placeholder |
| SideMenu | Custom | - | Complex sidebar navigation |
| SidePane | Custom | - | Docked side panel (desktop) / full-screen overlay (mobile) with accessibility. Exports: SidePane, SidePaneHeader, SidePaneBody, SidePaneFooter, SidePaneClose |
| Tabs | `npx shadcn@latest add tabs` | Outline focus ring, CSS variable heights, data-[active] instead of data-[selected] (BaseUI difference), active:bg-muted/50 | - |
| Sonner | `npx shadcn@latest add sonner` | Stock ShadCN (3rd party) | Third-party toast notification library |
| Table | `npx shadcn@latest add table` | Outline focus ring on TableRow, active:bg-muted, built-in keyboard navigation with roving tabindex (selectedIndex/onNavigate/onActivate on Table, index on TableRow) | - |
| TablePagination | Custom | - | Pagination wrapper for tables |
| TenantLogo | Custom | - | Avatar wrapper for tenant logos with square shape support |
| TextField | Custom | - | Field + Input + validation composition |
| Toggle | `npx shadcn@latest add toggle` | Outline focus ring, CSS variable heights, cursor-pointer, active:bg-accent | - |
| Tooltip | `npx shadcn@latest add tooltip` | Tap-to-open support for touch devices | - |
| UnsavedChangesAlertDialog | Custom | - | Unsaved changes confirmation |

---

## Global Divergence Patterns

All ShadCN components have these common modifications applied:

1. **Focus ring**: Ring utilities (`focus-visible:ring-*`) replaced with outline-based approach (`outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2`)
2. **Apple HIG compliance**: Fixed heights replaced with CSS variables (`--control-height` 44px, `--control-height-sm` 36px, `--control-height-xs` 28px). Smaller controls use `after:absolute after:-inset-3` for 44px tap targets
3. **Background colors**: `bg-transparent` replaced with `bg-white dark:bg-input/30` for explicit light-mode backgrounds
4. **Cursor pointer**: Clickable elements (buttons, toggles, checkboxes, radio buttons, select triggers, menu items) use `cursor-pointer`. ShadCN defaults to `cursor-default` on some elements
5. **Active state feedback**: Interactive elements have press feedback via `active:` pseudo-class with background color changes. Buttons/triggers and menu items use `active:bg-*` variants. Smaller controls (checkbox, radio) use `active:border-primary`

## Divergence Documentation

Components with divergences are documented in source code using:

```typescript
// NOTE: This diverges from stock ShadCN to [reason].
```

When reinstalling a component, reapply all documented divergences plus the global patterns above.
