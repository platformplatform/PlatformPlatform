# Form With Validation

Guidelines for implementing forms with validation in the frontend, covering UI components, mutation handling, and validation error display.

## Implementation

1. Use ShadCN components from `@repo/ui/components` for form elements
2. Use `api.useMutation` or TanStack's `useMutation` for form submissions
3. Use the custom `mutationSubmitter` to handle form submission and data mapping
4. Handle validation errors using the `validationErrors` prop from the mutation error
5. Show loading state in submit buttons using `disabled={mutation.isPending}`
6. For complex scenarios with multiple API calls, create a custom mutation with a `mutationFn`

## Anti-patterns

- **Do NOT use `<FormErrorMessage>`** - This component is deprecated. Instead:
  - Use `validationErrors` prop on the `<Form>` to show field-level validation errors
  - Use toast notifications to display server errors (non-validation errors)

Note: All .NET API endpoints are available as strongly typed API contracts in the frontend—when compiling the .NET backend, an OpenApi.json file is generated and the frontend build uses `openapi-typescript` to generate the API contracts.

## Examples

### Example 1 - Basic Form With Validation

```typescript
// ✅ DO: Use mutationSubmitter and proper error handling
import { api } from "@/shared/lib/api/client";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { Form, TextField, Button } from "@repo/ui/components";
import { Trans } from "@lingui/react/macro";

export function UserProfileForm({ user }) {
  const updateUserMutation = api.useMutation("put", "/api/account-management/users/me");
  
  return (
    <Form
      onSubmit={mutationSubmitter(updateUserMutation)}
      validationBehavior="aria"
      validationErrors={updateUserMutation.error?.errors}
    >
      <TextField
        autoFocus={true}
        isRequired={true}
        name="firstName"
        label={t`First name`}
        defaultValue={user?.firstName}
        placeholder={t`E.g., Alex`}
      />
      <TextField
        isRequired={true}
        name="lastName"
        label={t`Last name`}
        defaultValue={user?.lastName}
        placeholder={t`E.g., Taylor`}
      />
      
      <TextField
        name="title"
        label={t`Title`}
        defaultValue={user?.title}
      />

      <Button type="submit" disabled={updateUserMutation.isPending}>
        {updateUserMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
      </Button>
    </Form>
  );
}

// ❌ DON'T: Use direct form submission without mutationSubmitter
function BadUserProfileForm({ user }) {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  
  const handleSubmit = async (event) => {
    event.preventDefault();
    setIsLoading(true);
    
    try {
      const formData = new FormData(event.target);
      const data = Object.fromEntries(formData.entries());
      
      await fetch("/api/account-management/users/me", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
      });
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };
  
  return (
    <Form onSubmit={handleSubmit}>
      {/* Missing proper validation and error handling */}
      <TextField name="firstName" defaultValue={user?.firstName} isRequired />
      <TextField name="lastName" defaultValue={user?.lastName} isRequired />
      <TextField name="title" defaultValue={user?.title} />
      
      <Button type="submit" disabled={isLoading}>
        {isLoading ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
      </Button>
    </Form>
  );
}
```

### Example 2 - Complex Form With Multiple APIs Calls

```typescript
// ✅ DO: Use custom mutation for complex scenarios
export function UserProfileWithAvatarForm({ user, onSuccess, onClose }) {
  const [selectedAvatarFile, setSelectedAvatarFile] = useState(null);
  const [removeAvatar, setRemoveAvatar] = useState(false);
  
  const updateUserMutation = api.useMutation("put", "/api/account-management/users/me");
  const updateAvatarMutation = api.useMutation("post", "/api/account-management/users/me/avatar");
  const removeAvatarMutation = api.useMutation("delete", "/api/account-management/users/me/avatar");
  
  const queryClient = useQueryClient();
  
  // Complex mutation with multiple API calls
  const saveMutation = useMutation({
    mutationFn: async (data) => {
      // First API call - upload avatar if selected
      if (selectedAvatarFile) {
        const formData = new FormData();
        formData.append("file", selectedAvatarFile);
        await updateAvatarMutation.mutateAsync({ body: formData });
      } 
      
      // Second API call - remove avatar if requested
      else if (removeAvatar) {
        await removeAvatarMutation.mutateAsync({});
      }

      // Third API call - update user data
      return await updateUserMutation.mutateAsync(data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries();
      onSuccess?.();
      onClose?.();
    }
  });
  
  return (
    <Form
      onSubmit={mutationSubmitter(saveMutation)}
      validationBehavior="aria"
      validationErrors={saveMutation.error?.errors || updateUserMutation.error?.errors}
    >
      {/* Form fields */}

      <Button type="submit" disabled={saveMutation.isPending}>
        {saveMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
      </Button>
    </Form>
  );
}
```

