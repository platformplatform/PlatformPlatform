---
paths: **/*.tsx,**/*.ts,**/*.json,**/*.esproj,.editorconfig
description: Core rules for frontend TypeScript and React development
---

# Frontend

Guidelines for frontend TypeScript and React development, including component structure, code style, architecture patterns, and build/format steps.

## Code Navigation

Use LSP tools aggressively for code investigation: `goToDefinition`, `findReferences`, `hover`, `documentSymbol`. If LSP returns "No LSP server available", stop and instruct the user: `npm install -g typescript-language-server typescript`

## Browser Testing

Use browser MCP tools to test at `https://localhost:9000`. Use `UNLOCK` as OTP verification code (localhost only). Use `run` MCP tool to restart the server if needed (wait a few seconds after restart).

## Architecture Overview

1. **SPA Served by .NET Backend**:
   - SPA served via `SinglePageAppFallbackExtensions.cs` from the backend
   - UserInfo injected into HTML meta tags and available via `import.meta.user_info_env`
   - Authentication is server-side with HTTP-only cookies
   - YARP reverse proxy handles routing between SPA and APIs

2. **Module Federation for Micro-Frontends**:
   - Each self-contained system has its own WebApp
   - Common UI exposed via federation in `federated-modules/`
   - Shared components in `application/shared-webapp/`
   - Don't import directly between self-contained systems
   - Use `window.location.href` for navigation between systems (not TanStack Router)

3. **API Integration**:
   - API client auto-generated from OpenAPI spec
   - Located in `shared/lib/api/client.ts`
   - Never make direct fetch calls
   - Server state lives in TanStack Query only
   - Use `queryClient.invalidateQueries()` to refresh data after mutations

4. **ShadCN 2.0 with BaseUI** (not Radix UI):
   - **BaseUI** (`@base-ui/react`): Headless primitives providing accessibility and behavior
   - **ShadCN 2.0**: Pre-styled components built on BaseUI, using class-variance-authority (cva)
   - Install components via `npx shadcn add <component>` - never copy manually
   - After installing: change `@/utils` to `../utils` and rename file to PascalCase (e.g., `button.tsx` to `Button.tsx`)
   - **Never use `*:` or `**:` variants**: These Tailwind child/descendant variants generate `:is()` CSS selectors that the module federation CSS scoping plugin cannot scope, causing specificity bugs in production. In shared components, use `[&>*]:` or `[&_*]:` selectors instead. In application code, prefer putting the utility class directly on each child element (e.g., `className="max-sm:grow"` on each button instead of `max-sm:*:grow` on the parent)
   - **Focus ring**: Replace ShadCN's default `focus-visible:ring-*` / `focus-visible:border-ring` utilities with `outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2`. Use `outline-primary` or `outline-destructive` for colored variants instead of `outline-ring`
   - **Apple HIG compliance**: Interactive controls must use CSS variable heights: `h-[var(--control-height)]` (44px default), `h-[var(--control-height-sm)]` (36px), `h-[var(--control-height-xs)]` (28px). For controls like checkboxes/switches where 44px visual size is too large, ensure a 44px minimum tap target using `after:absolute after:-inset-3`
   - Import from `@repo/ui/components/`, never from BaseUI directly
   - Only create custom components when no ShadCN equivalent exists (edge cases)
   - **Cursor pointer**: Replace `cursor-default` with `cursor-pointer` on clickable elements
   - **Active state feedback**: Add press feedback to interactive elements using `active:` pseudo-class with background color changes. Buttons/triggers: `active:bg-primary/70` (or variant-specific active backgrounds). Menu/list items: `active:bg-accent`. Small controls (checkbox, radio): `active:border-primary`
   - **Use BaseUI `render` prop** to customize underlying elements (not Radix's `asChild`): `<DialogClose render={<Button />}>Close</DialogClose>`

## Implementation

1. Follow these code style and pattern conventions:
   - Use proper naming conventions:
     - PascalCase for components (e.g., `UserProfile`, `NavigationMenu`)
     - camelCase for variables and functions (e.g., `userName`, `handleSubmit`)
   - Create semantically correct components with clear boundaries and responsibilities:
     - Each component should have a single, well-defined purpose
     - UI elements with different functionality should be in separate components
     - Avoid mixing unrelated functionality in one component
   - Use clear, descriptive names instead of making comments
   - Don't use acronyms (e.g., use `errorMessage` not `errMsg`, `button` not `btn`, `authentication` not `auth`)
   - Prioritize code readability and maintainability
   - Don't introduce new npm dependencies
   - **Workspace package imports**: Within the same package (e.g., within `@repo/ui`), use relative imports (`../components/Button`) to avoid circular dependencies. Between different packages, use absolute imports (`@repo/ui/components/Button`)
   - Use ShadCN components instead of native HTML elements like `<a>`, `<button>`, `<fieldset>`, `<form>`, `<input>`, `<label>`, `<ol>`, `<p>`, `<progress>`, `<select>`, `<table>`, `<textarea>`, `<ul>` (native `<div>`, `<span>`, `<section>`, `<article>`, `<img>`, `<h1>`-`<h4>` are acceptable)
   - **Headings**: Use native `<h1>`-`<h4>` elements with global styles from `tailwind.css`. Never override font sizes or weights - use the correct semantic level for the visual hierarchy. Allowed overrides: alignment (`text-center`), margins (`mb-X`), visibility (`hidden sm:block`). Exception: Hero/marketing pages can override sizes
   - Use native `<img>` for images. Keep it simple for small logos/icons. For large images:
     - **LCP images** (large hero images): Add `fetchPriority="high"`
     - **Below-the-fold images**: Add `loading="lazy"`
     - Never use `width`/`height` HTML attributes -- use Tailwind `size-*` classes instead
     - Always include localized `alt` text using the `t` macro (e.g., `alt={t\`Description\`}`)
   - **Square dimensions**: Use Tailwind's `size-N` utility instead of `h-N w-N` for any square element (e.g., `size-4` not `h-4 w-4`). Only use separate `h-N w-N` for rectangular dimensions
   - **Rem-based sizing**: All sizes must use `rem`, never `px`. This enables UI scaling via the `--zoom-level` CSS variable while maintaining aspect ratios.
     - Tailwind arbitrary values: `max-w-[25rem]` not `max-w-[400px]`, `ml-[0.375rem]` not `ml-[6px]`
     - CSS variable fallbacks: `var(--banner-height,0rem)` not `var(--banner-height,0px)`
     - JS constants for CSS: `const BANNER_HEIGHT = "3rem"` not `const BANNER_HEIGHT = 48`
     - For JS pixel calculations, use `getSideMenuCollapsedWidth()` from `@repo/ui/utils/responsive`

2. Use the following React patterns and libraries:
   - Use ShadCN components from `@repo/ui/components/ComponentName`:
     - Search [Components](/application/shared-webapp/ui/components) when you need to find a component
     - Use existing components rather than creating new ones
   - Use `onClick` for click handlers and `disabled` for disabled state (ShadCN patterns)
   - Use `<Trans>...</Trans>` for JSX translations, `t` macro for strings
   - Use TanStack Query for API interactions via `api.useQuery()` and `api.useMutation()`
   - Don't use `fetch` directly - use the generated API client
   - Use Suspense boundaries with error boundaries at route level
   - Colocate state with components - don't lift state unnecessarily
   - Use `useCallback` and `useMemo` only for proven performance issues
   - Throw errors sparingly and ensure error messages include a period
   - Include appropriate aria labels for accessibility (e.g., `slot="title"` on Heading in dialogs)
   - Disable UI during pending operations: `disabled={mutation.isPending}` on buttons/fields, `isDismissable={!mutation.isPending}` on modals
   - **Dialog sizing**: Use `sm:w-dialog-*` utilities - never use custom widths like `max-w-lg` or arbitrary values

3. Error handling:
   - **Errors are handled globally** - `shared-webapp/infrastructure/http/errorHandler.ts` automatically shows toast notifications with the server's error message (don't manually show toasts for errors)
   - **Validation errors**: Pass to forms via `validationErrors={mutation.error?.errors}`
   - **`onError` is for UI cleanup only** (resetting loading states, closing dialogs), not for showing errors
   - **Toast notifications**: Show success toasts in mutation `onSuccess` callbacks, not in `useEffect` watching `isSuccess` (avoids React effect scheduling delays)

4. Responsive design utilities:
   - Use `useViewportResize()` hook to detect mobile viewport (returns `true` when mobile)
   - Use `isTouchDevice()` for touch vs mouse interactions
   - Use `isMediumViewportOrLarger()` for desktop-specific features

5. Z-index layering (don't invent new values):
   - `z-0` to `z-10`: **Content** -- sticky table headers, sticky toolbars, inline badges, calendar layers
   - `z-20`: **App bars** -- desktop top bar, mobile floating menu button
   - `z-30`: **Navigation + mobile header** -- side menu, mobile sticky header (stacks above content)
   - `z-[35]`: **Backdrops** -- dimmed overlays behind panels and overlay-mode navigation
   - `z-40`: **Panels** -- side panes, mobile full-screen menus, banners, side menu in overlay mode
   - `z-50`: **Popups** -- dialogs, dropdowns, popovers, tooltips (ShadCN default)
   - `z-100`: **Select popup** -- Select dropdown renders above dialogs (ShadCN default)
   - `z-[60]`: **Toasts** -- always visible, even above dialogs
   - `z-[99]`: **Critical** -- full-screen loaders, system overlays (e.g., account switching)

6. Dialog structure and DirtyDialog patterns:
   - **Always use DialogBody** for content between DialogHeader and DialogFooter - it provides proper scrolling for tall content
   - **X button**: Built-in close button shows unsaved warning if dirty
   - **Cancel button**: Use `<DialogClose render={<Button type="reset" .../>}>` - the `type="reset"` bypasses the warning
   - Always clear dirty state in `onSuccess` and `onCloseComplete`

7. **Empty states**: Use the `Empty` component with icon, title, and description when there is no content to display

8. **Loading states**: Use the `Skeleton` component to show placeholder UI while content is loading instead of spinners

9. Always follow these steps when implementing changes:
   - Consult relevant rule files and list which ones guided your implementation
   - Search the codebase for similar code before implementing new code
   - Reference existing implementations to maintain consistency

10. Build and format your changes:
    - After each minor change, use the **execute MCP tool** with `command: "build"` for frontend
    - This ensures consistent code style across the codebase

11. Verify your changes:
   - When a feature is complete, run these MCP tools for frontend in sequence: **build**, **format**, **inspect**
   - **ALL inspect findings are blocking** - CI pipeline fails on any result marked "Issues found"
   - Severity level (note/warning/error) is irrelevant - fix all findings before proceeding
   - Fix any compiler warnings or test failures before proceeding

## Examples

```tsx
// ✅ DO: Correct patterns
export function UserPicker({ isOpen, onOpenChange }: UserPickerProps) {
  const [isFormDirty, setIsFormDirty] = useState(false);
  const { data } = api.useQuery("get", "/api/account-management/users", { enabled: isOpen });
  const activeUsers = (data?.users ?? []).filter((u) => u.isActive); // ✅ Compute derived values inline

  const inviteMutation = api.useMutation("post", "/api/account-management/users/invite", {
    onSuccess: () => { // ✅ Show toast in onSuccess (not useEffect)
      setIsFormDirty(false);
      toast.success(t`Success`, { description: t`User invited` });
      onOpenChange(false);
    }
  });

  const handleCloseComplete = () => setIsFormDirty(false);

  return (
    <DirtyDialog open={isOpen} onOpenChange={onOpenChange} hasUnsavedChanges={isFormDirty}
                 onCloseComplete={handleCloseComplete}>
      <DialogContent className="sm:w-dialog-md"> // ✅ Use dialog width classes, rem for arbitrary values
        <DialogHeader>
          <DialogTitle><Trans>Select users</Trans></DialogTitle>
        </DialogHeader>
        <Form onSubmit={mutationSubmitter(inviteMutation)}>
          <DialogBody> // ✅ Always wrap content in DialogBody
            <TextField name="email" label={t`Email`} onChange={() => setIsFormDirty(true)} />
          </DialogBody>
          <DialogFooter>
            <DialogClose render={<Button type="reset" variant="secondary" disabled={inviteMutation.isPending} />}> // ✅ type="reset" bypasses warning
              <Trans>Cancel</Trans>
            </DialogClose>
            <Button type="submit" disabled={inviteMutation.isPending}> // ✅ Use disabled for pending
              {inviteMutation.isPending ? <Trans>Sending...</Trans> : <Trans>Send invite</Trans>}
            </Button>
          </DialogFooter>
        </Form>
      </DialogContent>
    </DirtyDialog>
  );
}

// ❌ DON'T: Common anti-patterns
function BadUserDialog({ users, selectedId, isOpen, onClose }) {
  const [filteredUsers, setFilteredUsers] = useState([]); // ❌ State for derived values
  const [isAdmin, setIsAdmin] = useState(false); // ❌ Duplicate state that can be calculated

  const inviteMutation = api.useMutation("post", "/api/users/invite");

  useEffect(() => { // ❌ useEffect for calculations - compute inline instead
    setFilteredUsers(users.filter(u => u.isActive));
    setIsAdmin(users.some(u => u.id === selectedId && u.role === "admin")); // ❌ Hardcode strings - use API contract types
  }, [users, selectedId]);

  useEffect(() => { // ❌ useEffect watching isSuccess causes toast timing issues
    if (inviteMutation.isSuccess) {
      toast.success("Success");
    }
  }, [inviteMutation.isSuccess]);

  const getDisplayName = useCallback((user) => { // ❌ Premature useCallback without performance need
    return `${user.firstName} ${user.lastName}`;
  }, []);

  const handleSelect = (id) => console.log(id); // ❌ "handle" + noun (use handleSelectUser), console.log

  return (
    <DirtyDialog open={isOpen} onOpenChange={onClose} hasUnsavedChanges={true}>
      <DialogContent className="sm:max-w-lg bg-white"> // ❌ max-w-lg (use w-dialog-md), hardcoded colors
        <h2>User Mgmt</h2> // ❌ Use DialogTitle in dialogs (not h2), acronym "Mgmt", missing <Trans>
        // ❌ Missing DialogBody wrapper - content won't scroll properly
        <ul> // ❌ Native <ul> - use ListBox
          {filteredUsers.map(user => (
            <li key={user.id} onClick={() => handleSelect(user.id)}> // ❌ Native <li>
              <img src={user.avatarUrl} /> // ❌ Native <img> - use Avatar
              <Text className="text-sm">{user.email}</Text> // ❌ text-sm with Text causes blur
              {getDisplayName(user)}
            </li>
          ))}
        </ul>
        <DialogFooter>
          <DialogClose render={<Button variant="secondary" />}> // ❌ Missing type="reset" (will show unwanted warning)
            Cancel
          </DialogClose>
          <Button type="submit"> // ❌ Missing disabled={isPending}
            <Trans>Submit</Trans> // ❌ Missing isPending text pattern, generic "Submit" text
          </Button>
        </DialogFooter>
      </DialogContent>
    </DirtyDialog>
  );
}

// ✅ DO: Rem-based sizing
const BANNER_HEIGHT = "3rem";
document.documentElement.style.setProperty("--banner-height", BANNER_HEIGHT);
document.documentElement.style.setProperty("--banner-height", "0rem"); // cleanup
<div className="max-w-[25rem] ml-[0.375rem]" />
<div className="pt-[calc(1rem+var(--banner-height,0rem))]" />

// ❌ DON'T: Px-based sizing
const BANNER_HEIGHT = 48;
document.documentElement.style.setProperty("--banner-height", `${BANNER_HEIGHT}px`);
document.documentElement.style.setProperty("--banner-height", "0px"); // cleanup
<div className="max-w-[400px] ml-[6px]" />
<div className="pt-[calc(1rem+var(--banner-height,0px))]" />
```
