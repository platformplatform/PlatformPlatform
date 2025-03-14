# TanStack Query API Integration

When integrating with backend APIs in the frontend, follow these rules very carefully.

## Implementation

1. Use TanStack Query hooks from the API utilities.
2. Leverage the strongly typed API contract generated from the .NET backend.
3. Use `api.useQuery` for data fetching operations.
4. Use `api.useMutation` for data modification operations.
5. Use `mutationSubmitter` for form submissions with mutations.

Note: All .NET API endpoints are available as strongly typed API contracts in the frontend. When compiling the .NET backend, an OpenApi.json file is generated, and the frontend build uses `openapi-typescript` to generate the API contracts.

```json
"scripts": {
  "swagger": "openapi-typescript shared/lib/api/AccountManagement.Api.json -o shared/lib/api/api.generated.d.ts --properties-required-by-default -t --enum --alphabetize",
},
```

## Example 1 - Data Fetching Example

```typescript
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

## Example 2 - Data Mutation Example

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

## Example 3 - Form Submission Example

```typescript
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
