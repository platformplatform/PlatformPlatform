import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { CheckCheckIcon, CheckIcon, ChevronDownIcon, type LucideIcon, SendIcon } from "lucide-react";

export type SendAction = "send" | "sendAndResolve" | "resolve";

interface SendActionConfig {
  icon: LucideIcon;
  pending: () => string;
  idle: () => string;
  // Whether the action requires a non-empty message body.
  needsBody: boolean;
  // Whether the action requires the message body AND attachment list to both be empty. Used by the
  // bare "Resolve" action — you can't resolve while still composing a reply.
  needsEmpty: boolean;
}

const SEND_ACTION_LABELS: Record<SendAction, SendActionConfig> = {
  send: { icon: SendIcon, pending: () => t`Sending…`, idle: () => t`Send`, needsBody: true, needsEmpty: false },
  sendAndResolve: {
    icon: CheckCheckIcon,
    pending: () => t`Sending…`,
    idle: () => t`Send & resolve`,
    needsBody: true,
    needsEmpty: false
  },
  resolve: { icon: CheckIcon, pending: () => t`Resolving…`, idle: () => t`Resolve`, needsBody: false, needsEmpty: true }
};

interface SplitSendButtonProps {
  primaryAction: SendAction;
  hasBody: boolean;
  hasAttachments: boolean;
  isPending: boolean;
  // When the ticket is terminal (Resolved/Closed) only "send" is offered — it reopens the ticket
  // (the composer confirms first). "Send & resolve" and bare "Resolve" are incoherent on a terminal
  // ticket (they would reopen then immediately re-resolve), so they are hidden.
  isTerminal: boolean;
  // Called when the user clicks the main button — executes the currently-selected action.
  onExecute: (action: SendAction) => void;
  // Called when the user picks a different action from the dropdown — only updates which action
  // the main button will execute, never fires the action immediately.
  onSelect: (action: SendAction) => void;
}

function isActionDisabled(config: SendActionConfig, hasBody: boolean, hasAttachments: boolean): boolean {
  if (config.needsBody && !hasBody) return true;
  if (config.needsEmpty && (hasBody || hasAttachments)) return true;
  return false;
}

export function SplitSendButton({
  primaryAction,
  hasBody,
  hasAttachments,
  isPending,
  isTerminal,
  onExecute,
  onSelect
}: Readonly<SplitSendButtonProps>) {
  const availableActions = (Object.keys(SEND_ACTION_LABELS) as SendAction[]).filter(
    (action) => !isTerminal || action === "send"
  );
  // A stale primaryAction (e.g. the ticket transitioned to terminal while composing) falls back to
  // the first available action so the main button never fires a hidden, incoherent action.
  const effectiveAction = availableActions.includes(primaryAction) ? primaryAction : availableActions[0];
  const config = SEND_ACTION_LABELS[effectiveAction];
  const Icon = config.icon;
  const disablePrimary = isPending || isActionDisabled(config, hasBody, hasAttachments);
  const showDropdown = availableActions.length > 1;

  return (
    <div className="flex items-stretch">
      <Button
        type="button"
        size="sm"
        className={showDropdown ? "rounded-r-none" : undefined}
        disabled={disablePrimary}
        isPending={isPending}
        onClick={() => onExecute(effectiveAction)}
      >
        <Icon className="size-3.5" />
        {isPending ? config.pending() : config.idle()}
      </Button>
      {showDropdown && (
        <DropdownMenu trackingTitle="Send actions">
          <Tooltip>
            <TooltipTrigger
              render={
                <DropdownMenuTrigger
                  render={
                    <Button
                      type="button"
                      size="sm"
                      className="-ml-px rounded-l-none px-2"
                      disabled={isPending}
                      aria-label={t`More send options`}
                    >
                      <ChevronDownIcon className="size-3.5" />
                    </Button>
                  }
                />
              }
            />
            <TooltipContent>{t`More send options`}</TooltipContent>
          </Tooltip>
          <DropdownMenuContent align="end">
            {availableActions.map((key) => {
              const value = SEND_ACTION_LABELS[key];
              const RowIcon = value.icon;
              return (
                <DropdownMenuItem
                  key={key}
                  onClick={() => onSelect(key)}
                  trackingLabel={key}
                  disabled={isActionDisabled(value, hasBody, hasAttachments)}
                >
                  <RowIcon className="size-4" />
                  <span className="flex-1">{value.idle()}</span>
                  {effectiveAction === key && <CheckIcon className="size-3.5" />}
                </DropdownMenuItem>
              );
            })}
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </div>
  );
}
