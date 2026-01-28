import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { MailQuestion } from "lucide-react";
import { useState } from "react";
import { SupportDialog } from "./SupportDialog";
import "@repo/ui/tailwind.css";

export default function SupportButton() {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <Tooltip>
        <TooltipTrigger
          render={
            <Button variant="ghost" size="icon" aria-label={t`Contact support`} onClick={() => setIsOpen(true)}>
              <MailQuestion className="size-5" />
            </Button>
          }
        />
        <TooltipContent>{t`Contact support`}</TooltipContent>
      </Tooltip>

      <SupportDialog isOpen={isOpen} onOpenChange={setIsOpen} />
    </>
  );
}
