import { Button, type ButtonProps } from "@repo/ui/components/Button";
import { useNavigate } from "@tanstack/react-router";
import { signOutPath } from "./constants";

type SignOutButtonProps = {
  customSignOutPath?: string;
} & Omit<ButtonProps, "onPress">;

export function SignOutButton({ customSignOutPath, children, ...props }: SignOutButtonProps) {
  const navigate = useNavigate();
  return (
    <Button {...props} onPress={() => navigate({ to: customSignOutPath ?? signOutPath })}>
      {children}
    </Button>
  );
}
