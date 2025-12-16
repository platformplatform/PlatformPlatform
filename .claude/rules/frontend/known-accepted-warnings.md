---
paths: *.tsx,*.ts
description: List of console warnings that are known issues and can be ignored
---

# Known Accepted Warnings

> **Human-Only File** - AI agents must never modify this file.

The following console warnings are known issues in third-party libraries or edge cases that are accepted and can be ignored. **No other warnings are acceptable.**

## Accepted Warnings List

### PressResponder Warnings

**Location**: `application/account-management/WebApp/routes/admin/account/index.tsx`
```
A PressResponder was rendered without a pressable child. Either call the usePress hook, or wrap your DOM node with <Pressable> component.
```

**Location**: `application/account-management/WebApp/federated-modules/common/UserProfileModal.tsx`
```
A PressResponder was rendered without a pressable child. Either call the usePress hook, or wrap your DOM node with <Pressable> component.
```

### Accessibility Warnings

**Location**: `application/account-management/WebApp/routes/admin/users`
```
Blocked aria-hidden on an element because its descendant retained focus. The focus must not be hidden from assistive technology users.
```

## Important Rules

1. **All other warnings must be fixed** - If you see a console warning not on this list, you must fix it before requesting review.
2. **Zero Tolerance** - The Boy Scout Rule applies: the codebase was clean when you started, so it must be clean when you finish.
3. **Verify warnings match exactly** - Check the file path and warning message match exactly before ignoring.
4. **When in doubt, fix it** - If a warning seems similar but does not match exactly, fix it.

