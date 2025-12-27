# Frontend

Guidelines for frontend TypeScript and React development, including component structure, code style, architecture patterns, and build/format steps.

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
   - Use `onPress` instead of `onClick` for event handlers (exception: Dialog close button uses `onClick={close}` from render prop)
   - Use `onAction` for menu items and list actions
   - Use `<Trans>...</Trans>` for JSX translations, `t` macro for strings
   - Use TanStack Query for API interactions via `api.useQuery()` and `api.useMutation()`
   - Don't use `fetch` directly—use the generated API client
   - Use Suspense boundaries with error boundaries at route level
   - Colocate state with components—don't lift state unnecessarily
   - Use `useCallback` and `useMemo` only for proven performance issues
   - Throw errors sparingly and ensure error messages include a period
   - Include appropriate aria labels for accessibility (e.g., `slot="title"` on Heading in dialogs)
   - Disable UI during pending operations: `isDisabled={mutation.isPending}` on buttons/fields, `isDismissable={!mutation.isPending}` on modals
   - Dialog sizing: `sm:w-dialog-md` (simple), `sm:w-dialog-lg` (4-6 fields), `sm:w-dialog-xl` (complex), `sm:w-dialog-2xl` (extra-large)

3. Error handling:
   - **Errors are handled globally**—`shared-webapp/infrastructure/http/errorHandler.ts` automatically shows toast notifications with the server's error message (don't manually show toasts for errors)
   - **Validation errors**: Pass to forms via `validationErrors={mutation.error?.errors}`
   - **`onError` is for UI cleanup only** (resetting loading states, closing dialogs), not for showing errors

4. Responsive design utilities:
   - Use `useViewportResize()` hook to detect mobile viewport (returns `true` when mobile)
   - Use `isTouchDevice()` for touch vs mouse interactions
   - Use `isMediumViewportOrLarger()` for desktop-specific features

5. Z-index layering for fixed-position elements (don't invent new values):
   - `z-0` to `z-20`: Content layers (sticky headers, table headers)
   - `z-30` to `z-40`: Navigation (top bar, mobile header)
   - `z-60`: Side menu collapsed
   - `z-70`: Side panes (backdrop at `z-[65]`)
   - `z-80`: Side menu expanded in overlay mode (backdrop at `z-[75]`)
   - `z-90`: Modal dialogs
   - `z-100`: High priority modals (nested, confirmations)
   - `z-[150]`: Toasts (always visible for user feedback)
   - `z-[200]`: Mobile full-screen menus
   - Note: Dropdowns, tooltips, and popovers use React Aria's overlay system which manages stacking relative to their context

6. Always follow these steps when implementing changes:
   - Consult relevant rule files and list which ones guided your implementation
   - Search the codebase for similar code before implementing new code
   - Reference existing implementations to maintain consistency

7. Build and format your changes:
   - After each minor change, use the **execute MCP tool** with `command: "build"` for frontend
   - This ensures consistent code style across the codebase

8. Verify your changes:
   - When a feature is complete, run these MCP tools for frontend in sequence: **build**, **format**, **inspect**
   - **ALL inspect findings are blocking** - CI pipeline fails on any result marked "Issues found"
   - Severity level (note/warning/error) is irrelevant - fix all findings before proceeding
   - Fix any compiler warnings or test failures before proceeding

## Examples

```tsx
// ✅ DO: Correct patterns
export function UserPicker({ isOpen, isPending, onOpenChange }: UserPickerProps) {
  const { data } = api.useQuery("get", "/api/account-management/users", { enabled: isOpen });
  const activeUsers = (data?.users ?? []).filter((u) => u.isActive); // ✅ Compute derived values inline

  const handleChangeSelection = (keys: Selection) => { /* ... */ }; // ✅ handleVerbNoun pattern

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={!isPending}> // ✅ Prevent dismiss during pending
      <Dialog className="sm:w-dialog-md"> // ✅ Use dialog width classes (not max-w-lg)
        {({ close }) => ( // ✅ Dialog render prop provides close function
          <>
            <XIcon onClick={close} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" /> // ✅ Close button pattern (onClick is exception)
            <DialogHeader description={t`Select users from the list.`}>
              <Heading slot="title" className="text-2xl"><Trans>Select users</Trans></Heading>
            </DialogHeader>
            <DialogContent>
              <ListBox aria-label={t`Users`} selectionMode="multiple" onSelectionChange={handleChangeSelection}>
                {activeUsers.map((user) => (
                  <ListBoxItem key={user.id} id={user.id}>
                    <Text>{`${user.firstName} ${user.lastName}`}</Text>
                  </ListBoxItem>
                ))}
              </ListBox>
            </DialogContent>
            <DialogFooter>
              <Button variant="primary" onPress={handleConfirm} isPending={isPending}> // ✅ Use isPending for loading
                <Trans>Confirm</Trans>
              </Button>
            </DialogFooter>
          </>
        )}
      </Dialog>
    </Modal>
  );
}

// ❌ DON'T: Common anti-patterns
function BadUserDialog({ users, selectedId, isOpen, onClose }) {
  const [filteredUsers, setFilteredUsers] = useState([]); // ❌ State for derived values
  const [isAdmin, setIsAdmin] = useState(false); // ❌ Duplicate state that can be calculated

  useEffect(() => { // ❌ useEffect for calculations - compute inline instead
    setFilteredUsers(users.filter(u => u.isActive));
    setIsAdmin(users.some(u => u.id === selectedId && u.role === "admin")); // ❌ Hardcode strings - use API contract types
  }, [users, selectedId]);

  const getDisplayName = useCallback((user) => { // ❌ Premature useCallback without performance need
    return `${user.firstName} ${user.lastName}`;
  }, []);

  const handleSelect = (id) => console.log(id); // ❌ "handle" + noun (use handleSelectUser), console.log

  return (
    <Modal isOpen={isOpen} onOpenChange={onClose}> // ❌ Missing isDismissable={!isPending}
      <Dialog className="sm:max-w-lg bg-white"> // ❌ max-w-lg (use w-dialog-md), hardcoded colors (use bg-background)
        <h1>User Mgmt</h1> // ❌ Native <h1> (use Heading), acronym "Mgmt", missing <Trans>
        <ul> // ❌ Native <ul> - use ListBox
          {filteredUsers.map(user => (
            <li key={user.id} onClick={() => handleSelect(user.id)}> // ❌ Native <li>, onClick (use onAction)
              <img src={user.avatarUrl} /> // ❌ Native <img> - use Avatar
              <Text className="text-sm">{user.email}</Text> // ❌ text-sm with Text causes blur
              {getDisplayName(user)}
            </li>
          ))}
        </ul>
        <Button onPress={handleSelect}>Submit</Button> // ❌ Missing isDisabled/isPending, missing <Trans>
      </Dialog>
    </Modal>
  );
}
```
