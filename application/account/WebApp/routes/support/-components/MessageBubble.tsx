import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { Logo } from "@repo/ui/components/Logo";
import { formatFileSize } from "@repo/ui/support/attachments";
import { PaperclipIcon } from "lucide-react";

import { SmartDate } from "@/shared/components/SmartDate";
import { type Schemas, SupportMessageAuthorKind } from "@/shared/lib/api/client";

type TicketMessage = Schemas["TicketMessageView"];

function getInitials(name: string): string {
  return name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
}

export function MessageBubble({ message }: { message: TicketMessage }) {
  const isOwnMessage = message.authorKind === SupportMessageAuthorKind.User;
  const isStaff = message.authorKind === SupportMessageAuthorKind.Staff;
  const initials = getInitials(message.authorDisplayName) || "?";
  const userInfo = useUserInfo();

  return (
    <div className={`flex items-start gap-2.5 ${isOwnMessage ? "flex-row-reverse" : ""}`}>
      {isStaff ? (
        <div
          className="flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-card"
          aria-label={t`Support staff`}
        >
          <Logo variant="mark" alt="" className="size-full" />
        </div>
      ) : (
        <Avatar size="lg" className="shrink-0">
          {isOwnMessage && <AvatarImage src={userInfo?.avatarUrl ?? undefined} alt="" />}
          <AvatarFallback>{initials}</AvatarFallback>
        </Avatar>
      )}
      <div className={`flex max-w-[75%] min-w-0 flex-col gap-1.5 ${isOwnMessage ? "items-end" : "items-start"}`}>
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <span className="font-medium text-foreground">
            {isOwnMessage ? <Trans>You</Trans> : message.authorDisplayName}
          </span>
          {isStaff && (
            <span className="inline-flex items-center gap-1 rounded-full bg-primary/10 px-1.5 py-0.5 text-[0.6875rem] font-medium text-primary">
              <Trans>Staff</Trans>
            </span>
          )}
          <span aria-hidden={true}>·</span>
          <SmartDate date={message.createdAt} />
        </div>
        <div
          className={`rounded-2xl border px-4 py-3 text-sm leading-relaxed whitespace-pre-wrap ${
            isOwnMessage ? "border-foreground bg-foreground text-background" : "border-border bg-card text-foreground"
          }`}
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
                    isOwnMessage
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
                  <span className={isOwnMessage ? "opacity-70" : "text-muted-foreground"}>
                    · {formatFileSize(attachment.sizeInBytes)}
                  </span>
                </Button>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
