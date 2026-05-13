import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogMedia,
  AlertDialogTitle
} from "@repo/ui/components/AlertDialog";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { Trash2Icon } from "lucide-react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

interface DeleteFeatureFlagDialogProps {
  flagKey: string;
  flagName: string;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function DeleteFeatureFlagDialog({
  flagKey,
  flagName,
  isOpen,
  onOpenChange
}: Readonly<DeleteFeatureFlagDialogProps>) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const deleteMutation = api.useMutation("delete", "/api/back-office/feature-flags/{flagKey}", {
    onSuccess: async () => {
      toast.success(t`Feature flag deleted: ${flagName}`);
      await queryClient.invalidateQueries({
        predicate: (query) =>
          Array.isArray(query.queryKey) &&
          query.queryKey[0] === "get" &&
          query.queryKey[1] === "/api/back-office/feature-flags"
      });
      onOpenChange(false);
      navigate({ to: "/feature-flags" });
    }
  });

  const handleDelete = () => {
    deleteMutation.mutate({ params: { path: { flagKey } } });
  };

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Delete feature flag">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-destructive/10">
            <Trash2Icon className="text-destructive" />
          </AlertDialogMedia>
          <AlertDialogTitle>
            <Trans>Delete feature flag</Trans>
          </AlertDialogTitle>
          <AlertDialogDescription>
            <Trans>
              Permanently delete <b>{flagName}</b> ({flagKey}) and all account and user overrides. This cannot be
              undone.
            </Trans>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary" disabled={deleteMutation.isPending}>
            <Trans>Cancel</Trans>
          </AlertDialogCancel>
          <AlertDialogAction variant="destructive" isPending={deleteMutation.isPending} onClick={handleDelete}>
            <Trans>Delete flag and all overrides</Trans>
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
