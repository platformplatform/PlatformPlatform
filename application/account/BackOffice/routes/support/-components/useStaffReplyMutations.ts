import { t } from "@lingui/core/macro";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { apiClient, type Schemas } from "@/shared/lib/api/client";

export function useStaffReplyMutations(ticketId: string, onSuccess: () => void) {
  const queryClient = useQueryClient();

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/support-tickets"] });
    queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/support-tickets/{id}"] });
  };

  const buildFormData = (body: string, files: File[], markAsResolved?: boolean) => {
    const formData = new FormData();
    formData.append("body", body);
    if (markAsResolved !== undefined) formData.append("markAsResolved", markAsResolved ? "true" : "false");
    for (const file of files) {
      formData.append("files", file);
    }
    return formData;
  };

  const replyMutation = useMutation<
    void,
    Schemas["HttpValidationProblemDetails"],
    { body: string; files: File[]; markAsResolved: boolean }
  >({
    mutationFn: async ({ body, files, markAsResolved }) => {
      // openapi-typescript reads [FromForm] as query params; we serialize the FormData verbatim.
      await apiClient.POST("/api/back-office/support-tickets/{id}/reply", {
        params: { path: { id: ticketId } },
        body: buildFormData(body, files, markAsResolved),
        bodySerializer: (value: unknown) => value as FormData
      } as never);
    },
    onSuccess: () => {
      toast.success(t`Reply sent`);
      invalidate();
      onSuccess();
    }
  });

  const internalNoteMutation = useMutation<
    void,
    Schemas["HttpValidationProblemDetails"],
    { body: string; files: File[] }
  >({
    mutationFn: async ({ body, files }) => {
      await apiClient.POST("/api/back-office/support-tickets/{id}/internal-note", {
        params: { path: { id: ticketId } },
        body: buildFormData(body, files),
        bodySerializer: (value: unknown) => value as FormData
      } as never);
    },
    onSuccess: () => {
      toast.success(t`Internal note saved`);
      invalidate();
      onSuccess();
    }
  });

  const resolveMutation = useMutation<void, Schemas["HttpValidationProblemDetails"]>({
    mutationFn: async () => {
      await apiClient.POST("/api/back-office/support-tickets/{id}/mark-resolved", {
        params: { path: { id: ticketId } }
      });
    },
    onSuccess: () => {
      toast.success(t`Ticket resolved`);
      invalidate();
      onSuccess();
    }
  });

  return {
    replyMutation,
    internalNoteMutation,
    resolveMutation,
    isAnyPending: replyMutation.isPending || internalNoteMutation.isPending || resolveMutation.isPending
  };
}
