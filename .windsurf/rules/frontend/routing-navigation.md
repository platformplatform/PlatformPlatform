---
trigger: glob
description: Rules for routing and navigation in React SPAs
globs: *.tsx,*.ts
---

# Routing and Navigation

## File-based Routing with Tanstack Router

1. **Route Structure**:
   - Routes are defined in `routes/` directory following file-system conventions
   - `__root.tsx` - Root layout wrapper
   - `index.tsx` - Index route for a path
   - `$param.tsx` - Dynamic route parameter
   - `(group)/` - Route groups (don't affect URL)
   - `-components/` - Co-located components for a route

2. **Route Configuration**:
   ```tsx
   // routes/__root.tsx
   import { Outlet, createRootRoute } from "@tanstack/react-router";
   
   export const Route = createRootRoute({
     component: RootComponent
   });
   
   function RootComponent() {
     return (
       <AuthenticationProvider>
         <Outlet />
       </AuthenticationProvider>
     );
   }
   ```

3. **Navigation Patterns**:
   ```tsx
   // ✅ Internal navigation within same SCS
   import { useNavigate } from "@tanstack/react-router";
   
   const navigate = useNavigate();
   navigate({ to: "/admin/users", search: { page: 2 } });
   
   // ✅ Navigation between self-contained systems
   window.location.href = "/back-office/dashboard";
   
   // ❌ DON'T: Import routes from other SCS
   import { routes } from "@back-office/routes"; // Never do this
   ```

4. **Search Params Management**:
   ```tsx
   // ✅ Typed search params
   const { search, userRole, startDate } = useSearch({ strict: false });
   
   navigate({
     to: "/admin/users",
     search: (prev) => ({
       ...prev,
       userRole: "Admin",
       startDate: undefined // Remove param
     })
   });
   ```

5. **Route Loaders and Preloading**:
   ```tsx
   export const Route = createRoute({
     getParentRoute: () => rootRoute,
     path: "/users/$userId",
     loader: async ({ params }) => {
       // Prefetch data for route
       await queryClient.prefetchQuery({
         queryKey: ["user", params.userId],
         queryFn: () => api.get(`/users/${params.userId}`)
       });
     },
     component: UserDetail
   });
   ```

## Navigation Between Self-Contained Systems

1. **Cross-SCS Navigation**:
   - Use full URL navigation: `window.location.href = "/other-scs/path"`
   - Never use Tanstack Router for cross-SCS navigation
   - Authentication context is preserved via HTTP-only cookies

2. **Shared State Across SCS**:
   - UserInfo is available globally via meta tags
   - Use Module Federation for shared UI components
   - Never share runtime state directly

## Link Components

```tsx
// ✅ Use React Aria Link with Tanstack Router
import { Link } from "@repo/ui/components/Link";
import { Link as RouterLink } from "@tanstack/react-router";

<Link href="/users">
  <RouterLink to="/users">View Users</RouterLink>
</Link>

// ❌ DON'T: Use anchor tags
<a href="/users">View Users</a>
```

## Protected Routes

```tsx
// Use authentication middleware
import { AuthenticationMiddleware } from "@repo/infrastructure/auth/AuthenticationMiddleware";

export const Route = createRoute({
  beforeLoad: AuthenticationMiddleware.requireAuth,
  component: ProtectedComponent
});
```

## Common Pitfalls

1. **DON'T mix routing libraries** - Use only Tanstack Router
2. **DON'T hardcode URLs** - Use router methods for internal navigation
3. **DON'T share route definitions** between self-contained systems
4. **DON'T use hash routing** - Use browser history
5. **DON'T forget loading states** for route transitions