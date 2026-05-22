import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { Logo } from "@repo/ui/components/Logo";
import { formatFileSize } from "@repo/ui/support/attachments";
import { EyeOffIcon, LockIcon, PaperclipIcon } from "lucide-react";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { type Schemas, SupportMessageAuthorKind } from "@/shared/lib/api/client";

import { getInitials } from "./displayName";

type StaffMessage = Schemas["StaffTicketMessageView"];

interface MessageBubbleProps {
  message: StaffMessage;
  // Reporter's avatar URL, available from the parent ticket detail response. Pulled out here so the
  // chat shows the actual face on user-authored messages instead of just initials.
  reporterAvatarUrl?: string | null;
}

export function MessageBubble({ message, reporterAvatarUrl }: Readonly<MessageBubbleProps>) {
  const isStaff = message.authorKind === SupportMessageAuthorKind.Staff;
  const isInternal = message.authorKind === SupportMessageAuthorKind.Internal;
  const isStaffSide = isStaff || isInternal;
  const initials = getInitials(message.authorDisplayName);

  return (
    <div className={`flex items-start gap-2.5 ${isStaffSide ? "flex-row-reverse" : ""}`}>
      {isStaff ? (
        <div
          className="flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-card"
          aria-label={t`Support staff`}
        >
          <Logo variant="mark" alt="" className="size-full" />
        </div>
      ) : (
        <Avatar size="lg" className="shrink-0">
          {!isStaffSide && <AvatarImage src={reporterAvatarUrl ?? undefined} alt="" />}
          <AvatarFallback className={isInternal ? "bg-warning/15 text-warning" : undefined}>{initials}</AvatarFallback>
        </Avatar>
      )}
      <div className={`flex max-w-[75%] min-w-0 flex-col gap-1.5 ${isStaffSide ? "items-end" : "items-start"}`}>
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <span className="font-medium text-foreground">{message.authorDisplayName}</span>
          {isInternal ? (
            <span className="inline-flex items-center gap-1 rounded-full bg-warning/15 px-1.5 py-0.5 text-[0.6875rem] font-medium text-warning">
              <LockIcon className="size-2.5" aria-hidden={true} />
              <Trans>Internal note</Trans>
            </span>
          ) : isStaff ? (
            <span className="inline-flex items-center gap-1 rounded-full bg-primary/10 px-1.5 py-0.5 text-[0.6875rem] font-medium text-primary">
              <Trans>Staff</Trans>
            </span>
          ) : (
            <span className="text-[0.6875rem]">
              <Trans>Account user</Trans>
            </span>
          )}
          <span aria-hidden={true}>·</span>
          <SmartDateTime date={message.postedAt} />
        </div>
        <div
          className={
            isInternal
              ? "rounded-2xl border border-dashed border-warning/40 bg-warning/5 px-4 py-3 text-sm leading-relaxed whitespace-pre-wrap text-foreground"
              : isStaff
                ? "rounded-2xl border border-foreground bg-foreground px-4 py-3 text-sm leading-relaxed whitespace-pre-wrap text-background"
                : "rounded-2xl border border-border bg-card px-4 py-3 text-sm leading-relaxed whitespace-pre-wrap text-foreground"
          }
        >
          {message.body}
          {message.attachments.length > 0 && (
            <div className="mt-2 flex flex-col gap-1.5">
              {message.attachments.map((attachment) => (
                <Button
                  key={attachment.url}
                  variant="outline"
                  size="sm"
                  className={`h-auto justify-start gap-2 rounded-lg bg-transparent px-2.5 py-1.5 text-xs no-underline shadow-none hover:underline ${
                    isStaff
                      ? "border-background/40 text-background hover:bg-background/10"
                      : "border-border text-foreground hover:bg-transparent"
                  }`}
                  render={
                    <a
                      href={attachment.url}
                      download={attachment.fileName}
                      aria-label={t`Download attachment ${attachment.fileName}`}
                    />
                  }
                >
                  <PaperclipIcon className="size-3" aria-hidden={true} />
                  <span className="truncate">{attachment.fileName}</span>
                  <span className={isStaff ? "opacity-70" : "text-muted-foreground"}>
                    · {formatFileSize(attachment.sizeInBytes)}
                  </span>
                </Button>
              ))}
            </div>
          )}
          {isInternal && (
            <div className="mt-2 flex items-center gap-1 text-xs text-muted-foreground italic">
              <EyeOffIcon className="size-3" aria-hidden={true} />
              <Trans>Visible only to PlatformPlatform staff</Trans>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
