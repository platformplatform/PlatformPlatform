import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { MailQuestion } from "lucide-react";
import { useState } from "react";
import { SupportDialog } from "./SupportDialog";
import "@repo/ui/tailwind.css";

export default function SupportButton() {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <TooltipTrigger>
        <Button variant="ghost" size="icon-lg" aria-label={t`Contact support`} onClick={() => setIsOpen(true)}>
          <MailQuestion className="size-5" />
        </Button>
        <Tooltip>{t`Contact support`}</Tooltip>
      </TooltipTrigger>

      <SupportDialog isOpen={isOpen} onOpenChange={setIsOpen} />
    </>
  );
}
