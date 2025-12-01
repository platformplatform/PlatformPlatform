# Frontend

Guidelines for frontend TypeScript and React development, including component structure, code style, architecture patterns, and build/format steps.

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
   - Use React Aria Components instead of native HTML elements like `<a>`, `<button>`, `<fieldset>`, `<form>`, `<h1>`-`<h6>`, `<img>`, `<input>`, `<label>`, `<ol>`, `<p>`, `<progress>`, `<select>`, `<table>`, `<textarea>`, `<ul>` (native `<div>`, `<span>`, `<section>`, `<article>` are acceptable)

2. Use the following React patterns and libraries:
   - Use React Aria Components from `@repo/ui/components/ComponentName`:
     - Search [Components](/application/shared-webapp/ui/components) when you need to find a component
     - Use existing components rather than creating new ones
   - Use `onPress` instead of `onClick` for event handlers
   - Use `onAction` for menu items and list actions
   - Use `<Trans>...</Trans>` for JSX translations, `t` macro for strings
   - Use TanStack Query for API interactions via `api.useQuery()` and `api.useMutation()`
   - Don't use `fetch` directly—use the generated API client
   - Use Suspense boundaries with error boundaries at route level
   - Colocate state with components—don't lift state unnecessarily
   - Use `useCallback` and `useMemo` only for proven performance issues
   - Throw errors sparingly and ensure error messages include a period

3. Error handling:
   - **Errors are handled globally**—`shared-webapp/infrastructure/http/errorHandler.ts` automatically shows toast notifications with the server's error message (don't manually show toasts for errors)
   - **Validation errors**: Pass to forms via `validationErrors={mutation.error?.errors}`
   - **`onError` is for UI cleanup only** (resetting loading states, closing dialogs), not for showing errors

4. Responsive design utilities:
   - Use `useViewportResize()` hook to detect mobile viewport (returns `true` when mobile)
   - Use `isTouchDevice()` for touch vs mouse interactions
   - Use `isMediumViewportOrLarger()` for desktop-specific features

5. Always follow these steps when implementing changes:
   - Consult relevant rule files and list which ones guided your implementation
   - Search the codebase for similar code before implementing new code
   - Reference existing implementations to maintain consistency

6. Build and format your changes:
   - After each minor change, use the **execute MCP tool** with `command: "build"` for frontend
   - This ensures consistent code style across the codebase

7. Verify your changes:
   - When a feature is complete, run these MCP tools for frontend in sequence: **build**, **format**, **inspect**
   - Fix any compiler warnings or test failures before proceeding

## Examples

### Example 1 - Component Structure

```tsx
// ✅ DO: Create focused components with clear responsibilities
import { Trans } from "@lingui/react/macro";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuHeader, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { LogOutIcon, UserIcon } from "lucide-react";

export function AvatarMenu({ userInfo, onProfileClick, onLogoutClick }: AvatarMenuProps) {
  return (
    <Menu placement="bottom end">
      <MenuHeader>
        <div className="flex flex-row items-center gap-2">
          <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials} isRound={true} size="sm" />
          <div className="my-1 flex flex-col">
            <h2>{userInfo.fullName}</h2>
            <p className="text-muted-foreground">{userInfo.title ?? userInfo.email}</p>
          </div>
        </div>
      </MenuHeader>
      <MenuItem onAction={onProfileClick}>
        <UserIcon className="h-4 w-4" />
        <Trans>Profile</Trans>
      </MenuItem>
      <MenuSeparator />
      <MenuItem onAction={onLogoutClick}>
        <LogOutIcon className="h-4 w-4" />
        <Trans>Log out</Trans>
      </MenuItem>
    </Menu>
  );
}

// ❌ DON'T: Mix unrelated functionality in a single component
function BadAvatarMenu({ userInfo }) { // Bad: Mixing menu with logout functionality
  return (
    <div className="menu"> // ❌ DON'T: Use CSS styles instead of Tailwind
      <div className="header">
        <div className="flex flex-row items-center gap-2"> // ❌ Unnecessary nested <div>
          <div className="m-12">
            <img src={userInfo.avatarUrl} alt="User avatar" /> // ❌ DON'T: Use native <img>
            <h2>{userInfo.fullName}</h2>
          </div>
        </div>
      </div>
      <button onClick={() => showProfile()}> // ❌ DON'T: Use native <button>
        <i className="icon-user"></i> Profile
      </button>
      <button onClick={() => {
        // ❌ DON'T: Implement logout logic directly in component or call fetch directly
        fetch("/api/account-management/authentication/logout", { method: "POST" })
          .then(() => window.location.href = "/login");
      }}>
        <i className="icon-logout"></i> Log out
      </button>
    </div>
  );
}
```
