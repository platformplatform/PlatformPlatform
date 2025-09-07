---
trigger: glob
description: Accessibility and responsive design requirements for React applications
globs: *.tsx,*.ts
---

# Accessibility and Responsive Design

## Accessibility Requirements

### React Aria Components Mandate

**NEVER use native HTML elements directly**. Always use React Aria Components:

```tsx
// ✅ CORRECT - React Aria Components
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { TextField } from "@repo/ui/components/TextField";
import { Heading } from "@repo/ui/components/Heading";
import { Text } from "@repo/ui/components/Text";

// ❌ WRONG - Native HTML elements
<button>Click me</button>  // NEVER
<a href="/link">Link</a>    // NEVER
<input type="text" />       // NEVER
<h1>Title</h1>             // NEVER
<p>Paragraph</p>           // NEVER
```

### Keyboard Navigation

1. **All interactive elements must be keyboard accessible**:
   ```tsx
   // ✅ React Aria handles keyboard automatically
   <Button onPress={handleAction}>Action</Button>
   <Menu>
     <MenuItem onAction={handleSelect}>Option</MenuItem>
   </Menu>
   ```

2. **Focus Management**:
   ```tsx
   // ✅ React Aria manages focus correctly
   <Dialog>
     <Heading>Dialog Title</Heading>
     <TextField autoFocus /> {/* Focus on open */}
   </Dialog>
   ```

3. **Keyboard Shortcuts**:
   ```tsx
   // Use keyboard navigation hooks
   import { useKeyboardNavigation } from "@repo/ui/hooks/useKeyboardNavigation";
   
   const isKeyboardNav = useKeyboardNavigation();
   // Show focus rings only for keyboard users
   ```

### Screen Reader Support

1. **Semantic Structure**:
   ```tsx
   // ✅ Use semantic React Aria components
   <Article>
     <Heading level={1}>Main Title</Heading>
     <Section>
       <Heading level={2}>Section Title</Heading>
       <Text>Content</Text>
     </Section>
   </Article>
   ```

2. **Live Regions for Dynamic Content**:
   ```tsx
   // Announce async updates
   import { Toast } from "@repo/ui/components/Toast";
   
   // Automatically announces to screen readers
   <Toast>User successfully created</Toast>
   ```

3. **Descriptive Labels**:
   ```tsx
   // ✅ Always provide labels
   <TextField label="Email Address" />
   <Button aria-label="Delete user">
     <TrashIcon />
   </Button>
   ```

### ARIA Patterns

**DON'T manually add ARIA attributes** - React Aria handles this:

```tsx
// ❌ WRONG - Manual ARIA
<div role="button" tabIndex={0} aria-pressed="false">

// ✅ CORRECT - React Aria Components
<ToggleButton>Toggle</ToggleButton>
```

## Responsive Design

### Mobile-First Approach

1. **Breakpoint Detection**:
   ```tsx
   import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
   import { isSmallViewport, isMediumViewportOrLarger } from "@repo/ui/utils/responsive";
   
   function Component() {
     const isMobile = useViewportResize();
     
     if (isMobile) {
       return <MobileLayout />;
     }
     return <DesktopLayout />;
   }
   ```

2. **Responsive Utilities**:
   ```tsx
   // Tailwind responsive classes
   <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
   <Text className="text-sm md:text-base lg:text-lg">
   ```

### Touch vs Desktop Interactions

```tsx
import { isTouchDevice } from "@repo/ui/utils/responsive";

// Different interactions for touch
const interaction = isTouchDevice() ? "onPress" : "onHoverStart";

// Mobile: Infinite scroll
// Desktop: Pagination
const { data } = isMobile 
  ? useInfiniteUsers({ enabled: true })
  : api.useQuery("get", "/api/users", { 
      params: { query: { pageOffset } }
    });
```

### Responsive Tables

```tsx
// Mobile: Cards or lists
// Desktop: Tables
function UserList() {
  const isMobile = useViewportResize();
  
  if (isMobile) {
    return (
      <GridList items={users}>
        {(user) => (
          <Card>
            <Heading>{user.name}</Heading>
            <Text>{user.email}</Text>
          </Card>
        )}
      </GridList>
    );
  }
  
  return (
    <Table>
      <TableHeader>
        <Column>Name</Column>
        <Column>Email</Column>
      </TableHeader>
      <TableBody>
        {users.map(user => (
          <Row key={user.id}>
            <Cell>{user.name}</Cell>
            <Cell>{user.email}</Cell>
          </Row>
        ))}
      </TableBody>
    </Table>
  );
}
```

### Viewport-Specific Features

```tsx
// Show different UI based on viewport
const showSidebar = isMediumViewportOrLarger();
const showMobileMenu = isSmallViewport();

// Conditional rendering
{showSidebar && <Sidebar />}
{showMobileMenu && <MobileMenu />}
```

## Performance for Accessibility

1. **Reduce Motion for Users with Preferences**:
   ```tsx
   // Respect prefers-reduced-motion
   className="transition-transform motion-reduce:transition-none"
   ```

2. **Lazy Load Heavy Components**:
   ```tsx
   const HeavyChart = lazy(() => import("./HeavyChart"));
   
   <Suspense fallback={<Skeleton />}>
     <HeavyChart />
   </Suspense>
   ```

3. **Image Optimization**:
   ```tsx
   import { Image } from "@repo/ui/components/Image";
   
   <Image 
     src={url}
     alt="Descriptive text" // Always required
     loading="lazy"
     sizes="(max-width: 768px) 100vw, 50vw"
   />
   ```

## Testing Accessibility

1. **Keyboard Testing**: Navigate entire app with keyboard only
2. **Screen Reader Testing**: Test with NVDA/JAWS (Windows) or VoiceOver (Mac)
3. **Automated Testing**: Use axe-core in tests
4. **Color Contrast**: Ensure WCAG AA compliance (4.5:1 for normal text)
5. **Focus Indicators**: Visible focus rings for all interactive elements

## Common Accessibility Violations to Avoid

1. **Missing labels on form inputs**
2. **Inaccessible custom dropdowns/modals**
3. **Color as the only indicator**
4. **Missing alt text on images**
5. **Keyboard traps**
6. **Auto-playing media**
7. **Time limits without warnings**
8. **Non-semantic markup**
9. **Missing skip links**
10. **Inaccessible error messages**