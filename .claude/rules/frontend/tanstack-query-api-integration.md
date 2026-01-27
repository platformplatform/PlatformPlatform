---
paths: **/*.tsx
description: Rules for using TanStack Query with backend APIs
---

# TanStack Query API Integration

Guidelines for integrating with backend APIs in the frontend using TanStack Query, covering data fetching, mutation, and API contract usage.

## Implementation

1. Use TanStack Query for all API integration
2. Use the shared-webapp library for common hooks and API clients
3. Leverage the strongly typed API contract generated from the .NET backend
4. Use `api.useQuery` for data fetching operations
5. Use `api.useMutation` for data modification operations
6. Use `mutationSubmitter` for form submissions with mutations
7. Use mutation's `onSuccess` callback for success toasts—never `useEffect` with `isSuccess` (causes duplicate toasts on re-renders)

Note: All .NET API endpoints are available as strongly typed API contracts in the frontend—when compiling the .NET backend, an OpenApi.json file is generated and the frontend build uses `openapi-typescript` to generate the API contracts.

```json
"scripts": {
  "swagger": "openapi-typescript shared/lib/api/AccountManagement.Api.json -o shared/lib/api/api.generated.d.ts --properties-required-by-default -t --enum --alphabetize",
},
```

## Examples

### Example 1 - Data Fetching Example

```typescript
import { LoadingSpinner } from "@repo/ui/components/LoadingSpinner";
import { UserList } from "@repo/ui/components/UserList";

const { data: users, isLoading } = api.useQuery("get", "/api/users", {
  params: { query: { Search: search } }
});

// ✅ TanStack Query options (enabled, staleTime) go in the 4th arg, not the 3rd
api.useQuery("get", "/api/users/{id}", { params: { path: { id } } }, { enabled: !!id });
// ❌ `enabled` in 3rd arg is silently ignored (becomes part of query key instead)
api.useQuery("get", "/api/users/{id}", { params: { path: { id } }, enabled: !!id });

// Usage in component
{isLoading ? (
  <LoadingSpinner />
) : (
  <UserList users={users} />
)}
```

### Example 2 - Data Mutation Example

```typescript
const completeLoginMutation = api.useMutation("post", "/api/login/{id}/complete");

const handleComplete = () => {
  completeLoginMutation.mutate(
    { 
      path: { id: loginId },
      body: { oneTimePassword: "123456" }
    },
    {
      onSuccess: () => {
        // Handle success
      },
      onError: (error) => {
        // Handle error
      }
    }
  );
};
```

### Example 3 - Form Submission Example

```typescript
import { Form } from "@repo/ui/components/Form";
import { TextField } from "@repo/ui/components/TextField";
import { Button } from "@repo/ui/components/Button";
import { Trans } from "@lingui/react/macro";

<Form
  onSubmit={mutationSubmitter(completeLoginMutation, {
    path: { id: loginId }
  })}
  validationErrors={completeLoginMutation.error?.errors}
>
  <TextField name="oneTimePassword" type="password" />
  <Button
    type="submit"
    disabled={completeLoginMutation.isPending}
  >
    {completeLoginMutation.isPending ? <Trans>Submitting...</Trans> : <Trans>Submit</Trans>}
  </Button>
</Form>
```

