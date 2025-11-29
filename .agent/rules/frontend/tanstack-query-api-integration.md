---
trigger: glob
globs: *.tsx
description: Rules for using TanStack Query with backend APIs
---
# TanStack Query API Integration

Carefully follow these instructions when integrating with backend APIs in the frontend using TanStack Query, covering data fetching, mutation, and API contract usage.

## Implementation

1. Use TanStack Query for all API integration
2. Use the shared-webapp library for common hooks and API clients
3. Leverage the strongly typed API contract generated from the .NET backend
4. Use `api.useQuery` for data fetching operations
5. Use `api.useMutation` for data modification operations
6. Use `mutationSubmitter` for form submissions with mutations

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
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
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
  <FormErrorMessage error={completeLoginMutation.error} />
  <Button 
    type="submit" 
    isDisabled={completeLoginMutation.isPending}
  >
    <Trans>Submit</Trans>
  </Button>
</Form>
```

