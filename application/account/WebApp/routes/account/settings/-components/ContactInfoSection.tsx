import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import type { ContactInfo } from "@repo/infrastructure/sync/hooks";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { Pencil } from "lucide-react";
import { useState } from "react";
import { useCountryName } from "@/shared/components/CountrySelect";
import EditContactInfoDialog from "./EditContactInfoDialog";

interface ContactInfoSectionProps {
  contactInfo: ContactInfo | null;
}

function ContactInfoRow({ label, value }: Readonly<{ label: string; value: string | null | undefined }>) {
  return (
    <div className="flex justify-between gap-4 py-1.5 text-sm sm:grid sm:grid-cols-[8rem_1fr]">
      <span className="text-muted-foreground">{label}</span>
      <span className={value ? "" : "text-muted-foreground italic"}>{value || t`Not provided`}</span>
    </div>
  );
}

export default function ContactInfoSection({ contactInfo }: Readonly<ContactInfoSectionProps>) {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const countryName = useCountryName(contactInfo?.country);

  return (
    <div className="mt-12 flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3>
          <Trans>Contact information</Trans>
        </h3>
        <Button variant="secondary" size="sm" onClick={() => setIsDialogOpen(true)}>
          <Pencil />
          <Trans>Edit</Trans>
        </Button>
      </div>
      <Separator />
      <div className="flex flex-col">
        <ContactInfoRow label={t`Phone`} value={contactInfo?.phoneNumber} />
        <ContactInfoRow label={t`Address`} value={contactInfo?.street} />
        <ContactInfoRow label={t`City`} value={contactInfo?.city} />
        <ContactInfoRow label={t`Zip code`} value={contactInfo?.postalCode} />
        <ContactInfoRow label={t`Country`} value={countryName} />
      </div>

      <EditContactInfoDialog isOpen={isDialogOpen} onOpenChange={setIsDialogOpen} contactInfo={contactInfo} />
    </div>
  );
}
