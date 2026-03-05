import { Trans } from "@lingui/react/macro";
import type { ContactInfo } from "@repo/infrastructure/sync/hooks";
import { Button } from "@repo/ui/components/Button";
import { MapPinIcon, Pencil, PhoneIcon } from "lucide-react";
import { useState } from "react";
import { useCountryName } from "@/shared/components/CountrySelect";
import EditContactInfoDialog from "./EditContactInfoDialog";

interface ContactInfoSectionProps {
  contactInfo: ContactInfo | null;
}

function hasAnyContactInfo(contactInfo: ContactInfo | null): boolean {
  if (!contactInfo) {
    return false;
  }
  return !!(
    contactInfo.address ||
    contactInfo.postalCode ||
    contactInfo.city ||
    contactInfo.state ||
    contactInfo.country ||
    contactInfo.phoneNumber
  );
}

export default function ContactInfoSection({ contactInfo }: Readonly<ContactInfoSectionProps>) {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const countryName = useCountryName(contactInfo?.country);
  const hasInfo = hasAnyContactInfo(contactInfo);

  const postalCodeCity = [contactInfo?.postalCode, contactInfo?.city].filter(Boolean).join(" ");
  const addressLines = [contactInfo?.address, postalCodeCity, contactInfo?.state, countryName].filter(Boolean);

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
      {hasInfo ? (
        <div className="flex flex-col gap-2 text-sm">
          {addressLines.length > 0 && (
            <div className="flex items-start gap-2">
              <MapPinIcon className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
              <div className="flex flex-col">
                {addressLines.map((line) => (
                  <span key={line}>{line}</span>
                ))}
              </div>
            </div>
          )}
          {contactInfo?.phoneNumber && (
            <div className="flex items-center gap-2">
              <PhoneIcon className="size-4 text-muted-foreground" />
              <span>{contactInfo.phoneNumber}</span>
            </div>
          )}
        </div>
      ) : (
        <p className="text-muted-foreground text-sm">
          <Trans>No contact information provided</Trans>
        </p>
      )}

      <EditContactInfoDialog isOpen={isDialogOpen} onOpenChange={setIsDialogOpen} contactInfo={contactInfo} />
    </div>
  );
}
