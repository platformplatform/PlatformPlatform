---
trigger: glob
description: State management and data fetching patterns for React applications
globs: *.tsx,*.ts
---

# State Management and Data Fetching

## Core Principle: Server State vs Client State

1. **Server State** (from APIs):
   - Managed EXCLUSIVELY by Tanstack Query
   - NEVER duplicate in local state, Context, or global stores
   - Single source of truth for all server data

2. **Client State** (UI-only):
   - Component state for UI interactions
   - Context for cross-component UI state
   - No global state management libraries (Redux, Zustand, etc.)

## Data Fetching with Tanstack Query

### API Client Usage

```tsx
// ✅ ALWAYS use the generated API client
import { api, queryClient } from "@/shared/lib/api/client";

// Query data
const { data, isLoading, error } = api.useQuery("get", "/api/account-management/users", {
  params: {
    query: { search, userRole, pageOffset }
  }
});

// Mutation
const updateUserMutation = api.useMutation("put", "/api/account-management/users/{id}", {
  onSuccess: () => {
    // Invalidate related queries
    queryClient.invalidateQueries({ queryKey: ["users"] });
  }
});

// ❌ NEVER use fetch directly
const data = await fetch("/api/users"); // DON'T DO THIS
```

### Query Key Patterns

```tsx
// ✅ Hierarchical query keys for cache invalidation
["users"] // All user queries
["users", "list", { search, page }] // User list with filters
["users", "detail", userId] // Specific user
["users", "detail", userId, "permissions"] // User permissions

// Invalidation patterns
queryClient.invalidateQueries({ queryKey: ["users"] }); // All user data
queryClient.invalidateQueries({ queryKey: ["users", "list"] }); // Just lists
```

### Optimistic Updates

```tsx
const mutation = api.useMutation("put", "/api/users/{id}", {
  onMutate: async (newData) => {
    // Cancel in-flight queries
    await queryClient.cancelQueries({ queryKey: ["users", id] });
    
    // Snapshot previous value
    const previousUser = queryClient.getQueryData(["users", id]);
    
    // Optimistically update
    queryClient.setQueryData(["users", id], newData);
    
    // Return context for rollback
    return { previousUser };
  },
  onError: (err, newData, context) => {
    // Rollback on error
    queryClient.setQueryData(["users", id], context.previousUser);
  },
  onSettled: () => {
    // Always refetch after error or success
    queryClient.invalidateQueries({ queryKey: ["users", id] });
  }
});
```

### Infinite Queries for Pagination

```tsx
// Mobile infinite scroll pattern
const {
  data,
  fetchNextPage,
  hasNextPage,
  isFetchingNextPage
} = api.useInfiniteQuery("get", "/api/users", {
  getNextPageParam: (lastPage) => lastPage.nextCursor,
  enabled: isMobile
});

// Desktop pagination
const { data } = api.useQuery("get", "/api/users", {
  params: { query: { pageOffset } },
  enabled: !isMobile
});
```

## Component State Management

### State Colocation

```tsx
// ✅ Keep state close to where it's used
function UserForm() {
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState(initialData);
  // State used only in this component
}

// ❌ DON'T lift state unnecessarily
function App() {
  const [userFormEditing, setUserFormEditing] = useState(false);
  // Bad: App doesn't need to know about form state
  return <UserForm isEditing={userFormEditing} />;
}
```

### Context for Cross-Component UI State

```tsx
// ✅ Context for UI state that multiple components need
const ThemeContext = createContext<Theme>("light");

export function ThemeProvider({ children }) {
  const [theme, setTheme] = useState<Theme>("light");
  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

// ❌ DON'T use Context for server state
const UserDataContext = createContext(); // Bad: Use Tanstack Query
```

## Authentication State

```tsx
// UserInfo is injected from backend and managed by AuthenticationProvider
import { useUserInfo } from "@repo/infrastructure/auth/hooks";

function Component() {
  const userInfo = useUserInfo();
  // UserInfo includes: email, firstName, lastName, tenantId, etc.
}

// Update user info after mutations
const updateProfileMutation = api.useMutation("put", "/api/users/me", {
  onSuccess: (data) => {
    updateUserInfo({
      firstName: data.firstName,
      lastName: data.lastName
    });
  }
});
```

## Form State

```tsx
// ✅ Use form libraries or React Aria forms
import { Form } from "@repo/ui/components/Form";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";

<Form onSubmit={mutationSubmitter(createUserMutation)}>
  <TextField name="email" isRequired />
  <Button type="submit">Create</Button>
</Form>

// ❌ DON'T manage form state manually
const [email, setEmail] = useState("");
const [name, setName] = useState("");
// Avoid manual state for each field
```

## Performance Considerations

1. **Query Deduplication**: Multiple components can use same query - automatically deduplicated
2. **Stale-While-Revalidate**: Show stale data immediately while fetching fresh data
3. **Background Refetching**: Queries refetch on window focus, network reconnect
4. **Selective Subscriptions**: Use `select` to subscribe to specific data

```tsx
// ✅ Select only needed data
const userName = api.useQuery("get", "/api/users/{id}", {
  params: { path: { id: userId } },
  select: (data) => data.name // Only re-render on name change
});
```

## Common Anti-Patterns to Avoid

1. **DON'T duplicate server state in useState**
2. **DON'T use useEffect for data fetching**
3. **DON'T mutate query cache directly (except optimistic updates)**
4. **DON'T forget error and loading states**
5. **DON'T create global stores for server data**
6. **DON'T mix different state management approaches**