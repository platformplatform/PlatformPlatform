import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Label } from "@repo/ui/components/Label";
import { Textarea } from "@repo/ui/components/Textarea";
import { TextField } from "@repo/ui/components/TextField";
import { useMutation } from "@tanstack/react-query";
import { Link as RouterLink, createFileRoute, useNavigate } from "@tanstack/react-router";
import { ArrowLeftIcon, ArrowRightIcon } from "lucide-react";
import { useState } from "react";

import { apiClient, type Schemas, SupportTicketCategory } from "@/shared/lib/api/client";

import { AttachmentChipList } from "../-components/AttachmentChipList";
import { categoryIcons } from "../-components/CategoryPill";
import { categoryLabels } from "../-components/statusMaps";

export const Route = createFileRoute("/support/tickets/new")({
  staticData: { trackingTitle: "Create support ticket" },
  component: CreateTicketPage
});

const categoryOrder: SupportTicketCategory[] = [
  SupportTicketCategory.Billing,
  SupportTicketCategory.Account,
  SupportTicketCategory.HowTo,
  SupportTicketCategory.Bug,
  SupportTicketCategory.Feature,
  SupportTicketCategory.Feedback,
  SupportTicketCategory.Other
];

function CreateTicketPage() {
  const { i18n } = useLingui();
  const userInfo = useUserInfo();
  const navigate = useNavigate();
  const [category, setCategory] = useState<SupportTicketCategory>(SupportTicketCategory.Billing);
  const [files, setFiles] = useState<File[]>([]);

  const createMutation = useMutation<
    Schemas["SupportTicketId"],
    Schemas["HttpValidationProblemDetails"],
    { subject: string; body: string }
  >({
    mutationFn: async ({ subject, body }) => {
      const formData = new FormData();
      formData.append("subject", subject);
      formData.append("body", body);
      formData.append("category", category);
      for (const file of files) {
        formData.append("files", file);
      }
      // Backend is a true multipart endpoint: text fields and files travel together as FormData.
      // The openapi-typescript generator misreads `[FromForm]` parameters as `query` params, so we
      // bypass the typed signature and serialize the FormData verbatim. Non-2xx responses are
      // thrown as ValidationError/ServerError by the shared HTTP middleware, so the field-level
      // errors land in `mutation.error.errors` and feed the Form's `validationErrors` directly.
      const response = await apiClient.POST("/api/account/support-tickets", {
        body: formData,
        bodySerializer: (value: unknown) => value as FormData
      } as never);
      return response.data as unknown as Schemas["SupportTicketId"];
    },
    onSuccess: (ticketId) => {
      navigate({ to: "/support/tickets/$ticketId", params: { ticketId: ticketId as unknown as string } });
    }
  });

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const subject = (formData.get("subject") as string | null) ?? "";
    const body = (formData.get("body") as string | null) ?? "";
    createMutation.mutate({ subject, body });
  };

  const tenantName = userInfo?.tenantName ?? "";
  const userName = userInfo?.fullName ?? userInfo?.email ?? "";

  return (
    <AppLayout
      variant="center"
      maxWidth="48rem"
      title={t`Tell us what's going on`}
      beforeHeader={
        <RouterLink
          to="/support/tickets"
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeftIcon className="size-3.5" aria-hidden={true} />
          <Trans>All tickets</Trans>
        </RouterLink>
      }
    >
      <Form
        onSubmit={handleSubmit}
        validationErrors={createMutation.error?.errors}
        validationBehavior="aria"
        className="flex flex-col gap-5"
      >
        <TextField
          autoFocus={true}
          name="subject"
          label={t`Subject`}
          placeholder={t`A short headline so we know what this is about`}
          required={true}
          disabled={createMutation.isPending}
        />

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="body">
            <Trans>What's happening?</Trans>
          </Label>
          <Textarea
            id="body"
            name="body"
            rows={6}
            placeholder={t`The more detail you can give, the faster we can help. Steps to reproduce, what you expected, what actually happened…`}
            disabled={createMutation.isPending}
            required={true}
          />
        </div>

        <div className="flex flex-col gap-2">
          <Label>
            <Trans>Category</Trans>
          </Label>
          <div className="flex flex-wrap gap-1.5">
            {categoryOrder.map((value) => {
              const Icon = categoryIcons[value];
              const isSelected = category === value;
              return (
                <Button
                  key={value}
                  type="button"
                  size="sm"
                  variant={isSelected ? "default" : "outline"}
                  disabled={createMutation.isPending}
                  onClick={() => setCategory(value)}
                  aria-pressed={isSelected}
                >
                  <Icon className="size-3.5" />
                  {i18n._(categoryLabels[value])}
                </Button>
              );
            })}
          </div>
        </div>

        <div className="flex flex-col gap-2">
          <Label>
            <Trans>Attachments</Trans>
            <span className="ml-2 text-xs font-normal text-muted-foreground">
              <Trans>(optional, up to 5)</Trans>
            </span>
          </Label>
          <AttachmentChipList files={files} onFilesChange={setFiles} disabled={createMutation.isPending} />
        </div>

        <div className="flex flex-wrap items-center gap-3 pt-2">
          <div className="text-xs text-muted-foreground">
            {tenantName ? (
              <Trans>
                Sending as <span className="font-medium text-foreground">{userName}</span> · {tenantName}
              </Trans>
            ) : (
              <Trans>
                Sending as <span className="font-medium text-foreground">{userName}</span>
              </Trans>
            )}
          </div>
          <div className="flex-1" />
          <Button type="submit" isPending={createMutation.isPending}>
            {createMutation.isPending ? <Trans>Sending...</Trans> : <Trans>Send to support</Trans>}
            {!createMutation.isPending && <ArrowRightIcon />}
          </Button>
        </div>
      </Form>
    </AppLayout>
  );
}
