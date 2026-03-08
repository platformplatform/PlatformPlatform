import { createContext } from "react";

type DirtyDialogContextValue = {
  cancel: () => void;
  hasUnsavedChanges: boolean;
};

export const DirtyDialogContext = createContext<DirtyDialogContextValue | null>(null);
