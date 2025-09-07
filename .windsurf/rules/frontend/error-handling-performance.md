---
trigger: glob
description: Error handling and performance optimization patterns
globs: *.tsx,*.ts
---

# Error Handling and Performance

## Error Boundaries

### Route-Level Error Boundaries

Every route should be protected by error boundaries:

```tsx
// routes/__root.tsx
import { ErrorBoundary } from "@repo/ui/components/ErrorBoundary";
import { ErrorPage } from "@repo/infrastructure/errorComponents/ErrorPage";

export const Route = createRootRoute({
  errorComponent: ErrorPage,
  component: RootComponent
});

// Granular error boundaries for critical sections
function RootComponent() {
  return (
    <ErrorBoundary fallback={<ErrorMessage />}>
      <CriticalFeature />
    </ErrorBoundary>
  );
}
```

### API Error Handling

```tsx
// ✅ Handle errors in mutations
const mutation = api.useMutation("post", "/api/users", {
  onError: (error) => {
    // Type-safe error from API
    if (error.code === "USER_EXISTS") {
      showToast({ 
        title: t`User already exists.`,
        variant: "error" 
      });
    }
  }
});

// ✅ Display error states in queries
const { data, error, isError } = api.useQuery("get", "/api/users");

if (isError) {
  return <ErrorMessage error={error} />;
}
```

### Global Error Handling

```tsx
// bootstrap.tsx
import { setupGlobalErrorHandlers } from "@repo/infrastructure/http/errorHandler";

setupGlobalErrorHandlers(); // Catches unhandled errors
```

## Performance Optimization

### Code Splitting

1. **Route-Based Splitting** (Automatic with file-based routing):
   ```tsx
   // Each route is automatically code-split
   // routes/admin/users/index.tsx - separate bundle
   ```

2. **Component Lazy Loading**:
   ```tsx
   // ✅ Lazy load heavy components
   const HeavyChart = lazy(() => import("./components/HeavyChart"));
   const UserProfileModal = lazy(() => import("./modals/UserProfileModal"));
   
   <Suspense fallback={<Spinner />}>
     <HeavyChart data={data} />
   </Suspense>
   ```

3. **Module Federation Boundaries**:
   ```tsx
   // Federated modules are loaded on demand
   const FederatedTopMenu = lazy(() => 
     import("accountManagement/FederatedTopMenu")
   );
   ```

### React 18 Concurrent Features

```tsx
// ✅ Use transitions for non-urgent updates
import { useTransition, useDeferredValue } from "react";

function SearchResults({ query }) {
  const [isPending, startTransition] = useTransition();
  const deferredQuery = useDeferredValue(query);
  
  const { data } = api.useQuery("get", "/api/search", {
    params: { query: { q: deferredQuery } }
  });
  
  return (
    <div style={{ opacity: isPending ? 0.5 : 1 }}>
      {data?.results.map(/* ... */)}
    </div>
  );
}
```

### Memoization Patterns

```tsx
// ✅ Use memoization ONLY for expensive computations
const expensiveValue = useMemo(() => {
  return heavyComputation(data);
}, [data]);

// ✅ Memoize callbacks passed to many children
const handleClick = useCallback((id: string) => {
  navigate({ to: `/users/${id}` });
}, [navigate]);

// ❌ DON'T over-memoize simple values
const name = useMemo(() => user.firstName, [user]); // Unnecessary
```

### Query Performance

```tsx
// 1. Selective data fetching
const userName = api.useQuery("get", "/api/users/{id}", {
  params: { path: { id } },
  select: (data) => data.name // Only subscribe to name changes
});

// 2. Parallel queries
const [users, roles] = Promise.all([
  queryClient.fetchQuery({ queryKey: ["users"] }),
  queryClient.fetchQuery({ queryKey: ["roles"] })
]);

// 3. Prefetching
const prefetchUser = (userId: string) => {
  queryClient.prefetchQuery({
    queryKey: ["users", userId],
    queryFn: () => api.get(`/users/${userId}`)
  });
};

// 4. Stale time configuration
const { data } = api.useQuery("get", "/api/config", {
  staleTime: 5 * 60 * 1000, // Consider fresh for 5 minutes
  cacheTime: 10 * 60 * 1000 // Keep in cache for 10 minutes
});
```

### Bundle Size Optimization

```tsx
// ✅ Tree-shakeable imports
import { Button } from "@repo/ui/components/Button";
// NOT: import * as UI from "@repo/ui";

// ✅ Dynamic imports for large libraries
const loadChart = async () => {
  const { Chart } = await import("chart-library");
  return Chart;
};

// ✅ Conditional polyfills
if (!window.IntersectionObserver) {
  await import("intersection-observer");
}
```

### Image Performance

```tsx
import { Image } from "@repo/ui/components/Image";

<Image
  src={imageUrl}
  alt="Description"
  loading="lazy" // Lazy load below fold
  sizes="(max-width: 768px) 100vw, 50vw"
  srcSet={`
    ${imageUrl}?w=400 400w,
    ${imageUrl}?w=800 800w,
    ${imageUrl}?w=1200 1200w
  `}
/>
```

### List Virtualization

```tsx
// For long lists, use virtualization
import { VirtualList } from "@repo/ui/components/VirtualList";

<VirtualList
  items={thousandsOfItems}
  itemHeight={50}
  renderItem={(item) => <UserRow user={item} />}
/>
```

## Web Vitals Targets

Ensure your components meet these targets:

1. **LCP (Largest Contentful Paint)**: < 2.5s
2. **FID (First Input Delay)**: < 100ms  
3. **CLS (Cumulative Layout Shift)**: < 0.1
4. **INP (Interaction to Next Paint)**: < 200ms

## Common Performance Anti-Patterns

1. **Inline function definitions in render**:
   ```tsx
   // ❌ Creates new function every render
   <Button onPress={() => handleClick(id)}>
   
   // ✅ Stable reference
   <Button onPress={handleClick}>
   ```

2. **Large component trees without boundaries**:
   ```tsx
   // ❌ Everything re-renders
   <Context.Provider value={{ ...allTheThings }}>
   
   // ✅ Split contexts
   <UserContext.Provider>
     <ThemeContext.Provider>
   ```

3. **Synchronous heavy computations**:
   ```tsx
   // ❌ Blocks rendering
   const result = expensiveCalculation(data);
   
   // ✅ Defer or memoize
   const result = useMemo(() => expensiveCalculation(data), [data]);
   ```

4. **Missing loading states**:
   ```tsx
   // ❌ Jarring experience
   if (!data) return null;
   
   // ✅ Smooth transitions
   if (isLoading) return <Skeleton />;
   ```

5. **Unnecessary re-renders**:
   ```tsx
   // ❌ Parent state changes cause all children to re-render
   // ✅ Use React.memo for expensive pure components
   const ExpensiveChild = memo(({ data }) => {
     // Only re-renders when data changes
   });
   ```