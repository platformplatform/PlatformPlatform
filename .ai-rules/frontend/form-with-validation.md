# Form with Validation

When implementing forms with validation in the frontend, follow these rules very carefully.

## Implementation Guidelines

1. Use React Aria Components from `@repo/ui/components` for form elements.
2. Use `api.useMutation` or TanStack's `useMutation` for form submissions.
3. Use the custom `mutationSubmitter` to handle form submission and data mapping.
4. Handle validation errors using the `validationErrors` prop from the mutation error.
5. Show loading state in submit buttons.
6. Include a `FormErrorMessage` component to display validation errors.

Note: All .NET API endpoints are available as strongly typed API contracts in the frontend. When compiling the .NET backend, an OpenApi.json file is generated, and the frontend build uses `openapi-typescript` to generate the API contracts.

```json
"scripts": {
  "swagger": "openapi-typescript shared/lib/api/AccountManagement.Api.json -o shared/lib/api/api.generated.d.ts --properties-required-by-default -t --enum --alphabetize",
}
```

## Example 1 - User profile modal

### Setting up the Mutation

```typescript
import { api } from "@/shared/lib/api/client";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useMutation } from "@tanstack/react-query";
import { Form, FormErrorMessage, TextField, Button } from "@repo/ui/components";
import { Trans } from "@lingui/react/macro";

const updateUserMutation = api.useMutation("put", "/api/account-management/users/me");

// ...

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
  
  {/* Error message display */}
  <FormErrorMessage error={updateUserMutation.error} />
  
  <Button type="submit" isDisabled={updateUserMutation.isPending}>
    {updateUserMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
  </Button>
</Form>
```

## Example 2 - Advanced Example - Complex Scenarios

For more complex scenarios where you need to perform multiple API calls or additional logic:

```typescript
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
    else if (removeAvatarFlag) {
      await removeAvatarMutation.mutateAsync({});
    }

    // Third API call - update user data
    await updateCurrentUserMutation.mutateAsync(data);
    
    // Additional logic after successful submission
    const { data: updatedUser } = await refetch();
    if (updatedUser) {
      updateUserInfo(updatedUser);
    }
    
    // UI cleanup
    closeDialog();
  }
});
```

## Key Points

1. Form Structure: Use the `Form` component with `onSubmit` handler using `mutationSubmitter`.
2. Validation: Pass `validationErrors` from the mutation error to the form.
3. Error Handling: Include `FormErrorMessage` to display validation errors.
4. Loading States: Show loading state in the submit button with `isDisabled={mutation.isPending}`.
5. Translations: Use `<Trans>` for text content and t-string literals for attributes.
6. Complex Scenarios: For multiple API calls or additional logic, create a custom mutation with a `mutationFn`.
