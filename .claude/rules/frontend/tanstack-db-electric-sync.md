---
paths: **/*.tsx,**/*.ts
description: Rules for TanStack DB live queries with Electric SQL sync for reading entity data
---

# TanStack DB Electric Sync

Guidelines for reading entity data via TanStack DB live queries backed by Electric SQL sync, covering data flow, hook usage, JSON column handling, and controlled input patterns.

## Implementation

1. Use the pre-built hooks in `@repo/infrastructure/sync/hooks` for reading entity data:
   - `useUsers()` -- all active users (list)
   - `useDeletedUsers()` -- soft-deleted users (recycle bin)
   - `useUser(userId)` -- single user by ID
   - `useTenant(tenantId)` -- single tenant by ID (includes plan)
   - `useSubscription(tenantId)` -- subscription with parsed billing data (Owner-only, returns empty for non-Owners)
   - `useSessions()` -- active sessions (list)

2. Understand the data flow: PostgreSQL -> Electric SQL sync -> TanStack DB collections -> `useLiveQuery` -> React components. Data arrives asynchronously -- on hard refresh, collections are empty until the Electric shape stream delivers rows

3. Follow these patterns for Electric-synced data in forms:
   - **Controlled inputs with `useEffect` sync**: For editable fields backed by Electric data, use `useState` + `useEffect` to sync from the hook data when the form is not dirty. This prevents losing user edits when Electric delivers updates
   - **Always provide string fallbacks for controlled inputs**: Use `value={data?.field ?? ""}` not `value={data?.field}`. Without the fallback, `undefined` makes React treat the input as uncontrolled, and when Electric data arrives later the value won't update
   - **Read-only display**: For non-editable display (labels, badges, cards), bind directly to hook data -- no state needed

4. Handle JSON columns correctly:
   - Electric delivers PostgreSQL JSON columns as raw strings, not parsed objects
   - The `snakeCamelMapper` only converts column names (snake_case to camelCase), NOT keys inside JSON column values
   - .NET EF Core serializes JSON value objects with PascalCase keys (e.g., `{"Brand":"visa","Last4":"4242"}`)
   - Use `castParsed<T>()` in hooks to parse JSON strings and convert PascalCase keys to camelCase
   - Use `extractUrl()` for JSON columns that contain a URL object (e.g., Avatar, Logo)

5. Prefer Electric data over static `userInfo` for display:
   - `useUserInfo()` returns static data injected into HTML at page load via `import.meta.user_info_env`
   - Components showing user/tenant data (menus, sidebars, profile cards) should subscribe to Electric hooks and fall back to `useUserInfo()` for initial render before Electric data arrives
   - After write mutations, Electric sync delivers the updated data automatically -- the UI updates without manual `updateUserInfo()` or `queryClient.invalidateQueries()` calls

6. Define types for parsed JSON columns locally in the hooks file:
   - Export interfaces (e.g., `PaymentMethod`, `BillingInfo`) from `hooks.ts` rather than depending on OpenAPI-generated types
   - Electric-synced entities may not have corresponding REST endpoints, so their types won't exist in the OpenAPI contract

7. Do NOT use `queryClient.invalidateQueries()` to refresh Electric-synced data:
   - Electric sync handles data freshness automatically
   - Only use `queryClient.invalidateQueries()` for REST-only data (e.g., pricing catalog, previews)

8. Use `useElectricMutation` from `@repo/infrastructure/sync/useElectricMutation` for all mutations that modify Electric-synced entities:
   - This hook bridges TanStack Query mutations with TanStack DB transactions and Electric SQL sync
   - It automatically sets `meta: { skipQueryInvalidation: true }` and awaits `electric-offset` sync confirmation
   - Pass `utils: collection.utils` to enable Electric sync awaiting via `awaitTxId`
   - Only use `api.useMutation` for mutations on REST-only data (e.g., sessions, Stripe webhooks)

9. Use the correct `useElectricMutation` path for each mutation type:
   - **Update/Delete** (optimistic): Provide `onMutate` -- runs inside a TanStack DB `createTransaction`. Collection changes apply instantly and auto-rollback on failure. Use `collection.update(id, (draft) => { draft.field = value; })` or `collection.delete(id)` inside `onMutate`
   - **Insert** (post-API): Provide `onInsert` -- called after `mutationFn` succeeds with the API response data. Guard with `collection.has(id)` to avoid `DuplicateKeyError` if Electric sync arrives first. Use `collection.insert({ ...data })` inside `onInsert`
   - **Plain** (no optimistic): Omit both `onMutate` and `onInsert` -- the hook calls `mutationFn` then awaits Electric sync via `awaitTxId`
   - Set properties individually in `collection.update()`, not via `Object.assign` (TanStack DB uses Immer-style proxies)
   - JSON value objects (Avatar, Logo) arrive from REST as parsed camelCase objects but Electric stores them as PascalCase JSON strings -- use `JSON.stringify()` when inserting and let Electric overwrite within milliseconds
   - The `extractUrl()` helper handles both PascalCase (`Url`) and camelCase (`url`) keys for this reason
   - Use `mutation.validationErrors` (not `mutation.error?.errors`) for form validation -- the hook provides a type-safe computed property

10. Show loading skeletons while Electric data is undefined:
    - Forms backed by Electric data (profile, account settings) must render `<Skeleton />` placeholders until the hook returns data
    - Without this, forms render with empty fields, and E2E tests fail because `fill("")` triggers no change event when the field is already empty
    - Check `data === undefined` (not `!data`) to distinguish "not loaded yet" from "loaded but empty"

11. Collection configuration in `collections.ts`:
   - `syncMode: "eager"` -- starts syncing immediately (tenants, subscriptions)
   - `syncMode: "on-demand"` -- syncs when a `useLiveQuery` first references the collection (users, sessions)
   - Collections using mutations must have `txidHandler` handlers: `onInsert: async () => txidHandler()`, `onUpdate: async () => txidHandler()`, `onDelete: async () => txidHandler()`. The `txidHandler` reads the `electric-offset` response header via `getLastElectricOffset()` and returns `{ txid }` for Electric's `processMatchingStrategy` to await. Without handlers, TanStack DB throws `MissingUpdateHandlerError` at runtime (TypeScript types make them optional, so the build will not catch this). Note: these handlers only fire for standalone collection ops -- when `collection.update()` runs inside `tx.mutate()`, it joins the ambient transaction and the handler is skipped

## Security

1. Understand the server-controlled security model:
   - The `ElectricShapeProxy` controls which columns are synced per table via server-side allowlists
   - The proxy strips any client-supplied `columns` and `where` parameters -- the server decides everything
   - Adding a new database column does NOT automatically expose it via Electric. It must be explicitly added to the server-side allowlist
   - Frontend code cannot request additional columns beyond what the proxy allows

2. Treat frontend access checks as UX helpers, not security boundaries:
   - Role-based restrictions (e.g., Owner-only billing pages) are enforced server-side by the proxy returning 403
   - Frontend route guards (`requirePermission`) improve UX by hiding inaccessible pages but are not security controls
   - Never rely solely on frontend checks to protect sensitive data -- the proxy is the enforcement layer

3. Follow the principle of least privilege for data access:
   - Each table's Electric shape syncs only the columns the frontend actually consumes
   - Sensitive columns (e.g., Stripe IDs, refresh tokens, IP addresses) are excluded from shapes
   - Some shapes are role-restricted (e.g., subscriptions require Owner role) or user-scoped (e.g., sessions filter by current user)
   - When adding a new UI feature, check whether the needed columns are in the shape allowlist before assuming they are available

4. Handle 403 responses from Electric gracefully:
   - When a user lacks the required role for a shape, the proxy returns 403
   - The collection will remain empty for unauthorized users -- this is expected behavior, not an error
   - Do not show error states for role-restricted shapes when the user lacks access -- show appropriate fallback UI or hide the section

5. Keep tenant-level data on the tenant shape for broad access:
   - Data needed by all tenant users (e.g., plan) belongs on the tenant shape
   - Data needed only by specific roles (e.g., billing details, payment method) belongs on role-restricted shapes
   - This avoids splitting the same table into multiple shapes for different roles

## Examples

### Example 1 - Editable Form With Electric Data

```typescript
// ✅ DO: useState + useEffect sync for editable fields
function ProfilePage() {
  const [firstName, setFirstName] = useState("");
  const [isFormDirty, setIsFormDirty] = useState(false);
  const { data: user } = useUser(userId);

  useEffect(() => {
    if (!isFormDirty && user?.firstName !== undefined) {
      setFirstName(user.firstName ?? "");
    }
  }, [user?.firstName, isFormDirty]);

  return (
    <TextField value={firstName} onChange={(v) => { setFirstName(v); setIsFormDirty(true); }} />
  );
}

// ❌ DON'T: defaultValue with Electric data (won't update after initial render)
function BadProfilePage() {
  const { data: user } = useUser(userId);
  return <TextField defaultValue={user?.firstName ?? ""} />; // Empty on hard refresh, never updates
}
```

### Example 2 - Read-Only Display With Fallback

```typescript
// ✅ DO: Always provide string fallback for controlled value
<TextField value={user?.email ?? ""} isDisabled={true} />

// ❌ DON'T: Omit fallback (undefined makes input uncontrolled)
<TextField value={user?.email} isDisabled={true} /> // Empty on hard refresh, won't update
```

### Example 3 - Preferring Electric Data Over Static UserInfo

```typescript
// ✅ DO: Check if Electric entity exists, then trust ALL its values (even null)
const userInfo = useUserInfo();
const { data: electricUser } = useUser(userInfo?.id ?? "");
const displayAvatarUrl = electricUser ? electricUser.avatarUrl : userInfo?.avatarUrl;
const displayEmail = electricUser ? electricUser.email : (userInfo?.email ?? "");

// ❌ DON'T: Use ?? (nullish coalescing) -- can't distinguish "not loaded" from "intentionally null"
const displayAvatarUrl = electricUser?.avatarUrl ?? userInfo?.avatarUrl; // Stale avatar after removal

// ❌ DON'T: Only read from static userInfo (stale after profile updates)
const displayName = userInfo?.fullName ?? "";
```

### Example 4 - Mutations With useElectricMutation

```typescript
// ✅ DO: Use useElectricMutation with onMutate for optimistic updates (auto-rollback on failure)
import { userCollection } from "@repo/infrastructure/sync/collections";
import { useElectricMutation } from "@repo/infrastructure/sync/useElectricMutation";
import { apiClient } from "@/shared/lib/api/client";

const changeRoleMutation = useElectricMutation({
  mutationFn: async (vars: { userId: string; role: string }) => {
    const { error } = await apiClient.PUT("/api/account/users/{id}/change-user-role", {
      params: { path: { id: vars.userId } },
      body: { role: vars.role }
    });
    if (error) throw error;
  },
  utils: userCollection.utils,
  onMutate: (vars) => {
    userCollection.update(vars.userId, (draft) => {
      draft.role = vars.role;
    });
  },
  onSuccess: () => {
    toast.success(t`Role updated`);
  }
});

// ✅ DO: Use onInsert for inserts (API first, then insert after success)
const inviteMutation = useElectricMutation({
  mutationFn: async (data: { body: { email: string } }) => {
    const { data: result, error } = await apiClient.POST("/api/account/users/invite", data);
    if (error) throw error;
    return result;
  },
  utils: userCollection.utils,
  onInsert: (data) => {
    if (!userCollection.has(data.id)) {
      userCollection.insert({ ...data, avatar: JSON.stringify(data.avatar) });
    }
  },
  onSuccess: () => {
    toast.success(t`User invited`);
  }
});

// ✅ DO: Use plain path (no onMutate/onInsert) when no optimistic update is needed
const saveMutation = useElectricMutation({
  mutationFn: async (data: { body: { name: string } }) => {
    const { error } = await apiClient.PUT("/api/account/tenants/current", data);
    if (error) throw error;
  },
  utils: tenantCollection.utils
});

// ❌ DON'T: Use api.useMutation for Electric-synced entities
const badMutation = api.useMutation("put", "/api/account/users/{id}", {
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ["get", "/api/account/users"] }); // Unnecessary with Electric
  }
});

// ❌ DON'T: Use mutation.error?.errors -- use validationErrors instead
<Form validationErrors={mutation.error?.errors}> // Wrong: HttpError union type issue
<Form validationErrors={mutation.validationErrors}> // Correct: type-safe computed property
```

### Example 5 - Loading Skeletons for Electric-Backed Forms

```typescript
// ✅ DO: Show skeleton while Electric data is loading
function ProfilePage() {
  const { data: user } = useUser(userId);

  return user === undefined ? (
    <div className="flex flex-col gap-4">
      <Skeleton className="h-16 w-full" />
      <Skeleton className="h-16 w-full" />
    </div>
  ) : (
    <Form>
      <TextField value={user.firstName ?? ""} />
    </Form>
  );
}

// ❌ DON'T: Render empty form fields while Electric data loads
function BadProfilePage() {
  const { data: user } = useUser(userId);
  return (
    <Form>
      <TextField value={user?.firstName ?? ""} /> {/* Empty on load, E2E fill("") is a no-op */}
    </Form>
  );
}
```

### Example 6 - Deriving Types From Electric Hooks

```typescript
// ✅ DO: Import types from the sync hooks
import type { BillingInfo, PaymentMethod } from "@repo/infrastructure/sync/hooks";

// ✅ DO: Derive row type from hook return type
type ElectricUser = ReturnType<typeof useUsers>["data"][number];

// ❌ DON'T: Import from OpenAPI for Electric-synced entities (types may not exist)
import type { components } from "@/shared/lib/api/api.generated";
type BillingInfo = components["schemas"]["BillingInfo"]; // Breaks when REST endpoint is removed
```
