---
trigger: glob
description: Rules for modal dialogs using React Aria Components
globs: *Dialog.tsx,*Modal.tsx
---

# Modal Dialog

Carefully follow these instructions when implementing modal dialogs in the frontend, focusing on accessibility, component usage, and translation patterns.

## Implementation

1. Use React Aria Components from `@repo/ui/components`.
2. Use the `Dialog` component from shared-webapp for all dialogs and modal dialogs.
3. Manage dialog state with React hooks (either `useState` or context depending on scope).
4. Use `onPress` instead of `onClick` for event handlers.
5. Include appropriate aria labels for accessibility.
6. Use `<Trans>...</Trans>` or t\`...\` for translations (content should be plain English).

## Examples

### Example 1 - Simple Dialog

```typescript
const [isOpen, setIsOpen] = useState(false);

const openDialog = () => setIsOpen(true);
const closeDialog = () => setIsOpen(false);

// Button to open the dialog
<Button onPress={openDialog}>
  <Trans>Open Dialog</Trans>
</Button>

// The dialog itself
<DialogContainer isOpen={isOpen} onDismiss={closeDialog}>
  <Dialog>
    <Heading>
      <Trans>Dialog Title</Trans>
    </Heading>
    <Content>
      <Text>
        <Trans>Dialog content goes here</Trans>
      </Text>
    </Content>
    <ButtonGroup>
      <Button variant="secondary" onPress={closeDialog}>
        <Trans>Cancel</Trans>
      </Button>
      <Button variant="primary" onPress={() => {
        // Action logic here
        closeDialog();
      }}>
        <Trans>Confirm</Trans>
      </Button>
    </ButtonGroup>
  </Dialog>
</DialogContainer>
```