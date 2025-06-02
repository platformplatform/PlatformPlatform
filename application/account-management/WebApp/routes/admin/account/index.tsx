import { type AddressData, AddressForm } from "@/shared/components/AddressForm";
import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import logoWrap from "@/shared/images/logo-wrap.svg";
import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { TextField } from "@repo/ui/components/TextField";
import { createFileRoute } from "@tanstack/react-router";
import { Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import { Header, Label, Section, Separator } from "react-aria-components";
import DeleteAccountConfirmation from "./-components/DeleteAccountConfirmation";

export const Route = createFileRoute("/admin/account/")({
  component: AccountSettings
});

export function AccountSettings() {
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedCountry, setSelectedCountry] = useState<string>("");
  const [address, setAddress] = useState<AddressData>({
    street: "",
    street2: "",
    city: "",
    state: "",
    zip: "",
    country: ""
  });

  const { data: tenant, isLoading } = api.useQuery("get", "/api/account-management/tenants/current");
  const updateCurrentTenantMutation = api.useMutation("put", "/api/account-management/tenants/current");

  useEffect(() => {
    if (tenant) {
      setSelectedCountry(tenant.country || "");
      setAddress({
        street: tenant.street || "",
        street2: "",
        city: tenant.city || "",
        state: tenant.addressState || "",
        zip: tenant.zip || "",
        country: tenant.country || ""
      });
    }
  }, [tenant]);

  const continents = [
    {
      name: t`Europe`,
      countries: [
        { code: "AD", name: t`Andorra` },
        { code: "AL", name: t`Albania` },
        { code: "AT", name: t`Austria` },
        { code: "BA", name: t`Bosnia and Herzegovina` },
        { code: "BE", name: t`Belgium` },
        { code: "BG", name: t`Bulgaria` },
        { code: "BY", name: t`Belarus` },
        { code: "CH", name: t`Switzerland` },
        { code: "CY", name: t`Cyprus` },
        { code: "CZ", name: t`Czech Republic` },
        { code: "DE", name: t`Germany` },
        { code: "DK", name: t`Denmark` },
        { code: "EE", name: t`Estonia` },
        { code: "ES", name: t`Spain` },
        { code: "FI", name: t`Finland` },
        { code: "FR", name: t`France` },
        { code: "GB", name: t`United Kingdom` },
        { code: "GR", name: t`Greece` },
        { code: "HR", name: t`Croatia` },
        { code: "HU", name: t`Hungary` },
        { code: "IE", name: t`Ireland` },
        { code: "IS", name: t`Iceland` },
        { code: "IT", name: t`Italy` },
        { code: "LI", name: t`Liechtenstein` },
        { code: "LT", name: t`Lithuania` },
        { code: "LU", name: t`Luxembourg` },
        { code: "LV", name: t`Latvia` },
        { code: "MC", name: t`Monaco` },
        { code: "MD", name: t`Moldova` },
        { code: "ME", name: t`Montenegro` },
        { code: "MK", name: t`North Macedonia` },
        { code: "MT", name: t`Malta` },
        { code: "NL", name: t`Netherlands` },
        { code: "NO", name: t`Norway` },
        { code: "PL", name: t`Poland` },
        { code: "PT", name: t`Portugal` },
        { code: "RO", name: t`Romania` },
        { code: "RS", name: t`Serbia` },
        { code: "RU", name: t`Russia` },
        { code: "SE", name: t`Sweden` },
        { code: "SI", name: t`Slovenia` },
        { code: "SK", name: t`Slovakia` },
        { code: "SM", name: t`San Marino` },
        { code: "UA", name: t`Ukraine` },
        { code: "VA", name: t`Vatican City` }
      ]
    },
    {
      name: t`North America`,
      countries: [
        { code: "AG", name: t`Antigua and Barbuda` },
        { code: "BS", name: t`Bahamas` },
        { code: "BB", name: t`Barbados` },
        { code: "BZ", name: t`Belize` },
        { code: "CA", name: t`Canada` },
        { code: "CR", name: t`Costa Rica` },
        { code: "CU", name: t`Cuba` },
        { code: "DM", name: t`Dominica` },
        { code: "DO", name: t`Dominican Republic` },
        { code: "SV", name: t`El Salvador` },
        { code: "GD", name: t`Grenada` },
        { code: "GT", name: t`Guatemala` },
        { code: "HT", name: t`Haiti` },
        { code: "HN", name: t`Honduras` },
        { code: "JM", name: t`Jamaica` },
        { code: "MX", name: t`Mexico` },
        { code: "NI", name: t`Nicaragua` },
        { code: "PA", name: t`Panama` },
        { code: "KN", name: t`Saint Kitts and Nevis` },
        { code: "LC", name: t`Saint Lucia` },
        { code: "VC", name: t`Saint Vincent and the Grenadines` },
        { code: "TT", name: t`Trinidad and Tobago` },
        { code: "US", name: t`United States` }
      ]
    },
    {
      name: t`South America`,
      countries: [
        { code: "AR", name: t`Argentina` },
        { code: "BO", name: t`Bolivia` },
        { code: "BR", name: t`Brazil` },
        { code: "CL", name: t`Chile` },
        { code: "CO", name: t`Colombia` },
        { code: "EC", name: t`Ecuador` },
        { code: "FK", name: t`Falkland Islands` },
        { code: "GF", name: t`French Guiana` },
        { code: "GY", name: t`Guyana` },
        { code: "PY", name: t`Paraguay` },
        { code: "PE", name: t`Peru` },
        { code: "SR", name: t`Suriname` },
        { code: "UY", name: t`Uruguay` },
        { code: "VE", name: t`Venezuela` }
      ]
    },
    {
      name: t`Asia`,
      countries: [
        { code: "AF", name: t`Afghanistan` },
        { code: "AM", name: t`Armenia` },
        { code: "AZ", name: t`Azerbaijan` },
        { code: "BH", name: t`Bahrain` },
        { code: "BD", name: t`Bangladesh` },
        { code: "BT", name: t`Bhutan` },
        { code: "BN", name: t`Brunei` },
        { code: "KH", name: t`Cambodia` },
        { code: "CN", name: t`China` },
        { code: "GE", name: t`Georgia` },
        { code: "HK", name: t`Hong Kong` },
        { code: "IN", name: t`India` },
        { code: "ID", name: t`Indonesia` },
        { code: "IR", name: t`Iran` },
        { code: "IQ", name: t`Iraq` },
        { code: "IL", name: t`Israel` },
        { code: "JP", name: t`Japan` },
        { code: "JO", name: t`Jordan` },
        { code: "KZ", name: t`Kazakhstan` },
        { code: "KW", name: t`Kuwait` },
        { code: "KG", name: t`Kyrgyzstan` },
        { code: "LA", name: t`Laos` },
        { code: "LB", name: t`Lebanon` },
        { code: "MO", name: t`Macao` },
        { code: "MY", name: t`Malaysia` },
        { code: "MV", name: t`Maldives` },
        { code: "MN", name: t`Mongolia` },
        { code: "MM", name: t`Myanmar` },
        { code: "NP", name: t`Nepal` },
        { code: "KP", name: t`North Korea` },
        { code: "OM", name: t`Oman` },
        { code: "PK", name: t`Pakistan` },
        { code: "PS", name: t`Palestine` },
        { code: "PH", name: t`Philippines` },
        { code: "QA", name: t`Qatar` },
        { code: "SA", name: t`Saudi Arabia` },
        { code: "SG", name: t`Singapore` },
        { code: "KR", name: t`South Korea` },
        { code: "LK", name: t`Sri Lanka` },
        { code: "SY", name: t`Syria` },
        { code: "TW", name: t`Taiwan` },
        { code: "TJ", name: t`Tajikistan` },
        { code: "TH", name: t`Thailand` },
        { code: "TL", name: t`Timor-Leste` },
        { code: "TR", name: t`Turkey` },
        { code: "TM", name: t`Turkmenistan` },
        { code: "AE", name: t`United Arab Emirates` },
        { code: "UZ", name: t`Uzbekistan` },
        { code: "VN", name: t`Vietnam` },
        { code: "YE", name: t`Yemen` }
      ]
    },
    {
      name: t`Africa`,
      countries: [
        { code: "DZ", name: t`Algeria` },
        { code: "AO", name: t`Angola` },
        { code: "BJ", name: t`Benin` },
        { code: "BW", name: t`Botswana` },
        { code: "BF", name: t`Burkina Faso` },
        { code: "BI", name: t`Burundi` },
        { code: "CV", name: t`Cape Verde` },
        { code: "CM", name: t`Cameroon` },
        { code: "CF", name: t`Central African Republic` },
        { code: "TD", name: t`Chad` },
        { code: "KM", name: t`Comoros` },
        { code: "CG", name: t`Congo` },
        { code: "CD", name: t`Democratic Republic of the Congo` },
        { code: "DJ", name: t`Djibouti` },
        { code: "EG", name: t`Egypt` },
        { code: "GQ", name: t`Equatorial Guinea` },
        { code: "ER", name: t`Eritrea` },
        { code: "SZ", name: t`Eswatini` },
        { code: "ET", name: t`Ethiopia` },
        { code: "GA", name: t`Gabon` },
        { code: "GM", name: t`Gambia` },
        { code: "GH", name: t`Ghana` },
        { code: "GN", name: t`Guinea` },
        { code: "GW", name: t`Guinea-Bissau` },
        { code: "CI", name: t`Ivory Coast` },
        { code: "KE", name: t`Kenya` },
        { code: "LS", name: t`Lesotho` },
        { code: "LR", name: t`Liberia` },
        { code: "LY", name: t`Libya` },
        { code: "MG", name: t`Madagascar` },
        { code: "MW", name: t`Malawi` },
        { code: "ML", name: t`Mali` },
        { code: "MR", name: t`Mauritania` },
        { code: "MU", name: t`Mauritius` },
        { code: "MA", name: t`Morocco` },
        { code: "MZ", name: t`Mozambique` },
        { code: "NA", name: t`Namibia` },
        { code: "NE", name: t`Niger` },
        { code: "NG", name: t`Nigeria` },
        { code: "RW", name: t`Rwanda` },
        { code: "ST", name: t`São Tomé and Príncipe` },
        { code: "SN", name: t`Senegal` },
        { code: "SC", name: t`Seychelles` },
        { code: "SL", name: t`Sierra Leone` },
        { code: "SO", name: t`Somalia` },
        { code: "ZA", name: t`South Africa` },
        { code: "SS", name: t`South Sudan` },
        { code: "SD", name: t`Sudan` },
        { code: "TZ", name: t`Tanzania` },
        { code: "TG", name: t`Togo` },
        { code: "TN", name: t`Tunisia` },
        { code: "UG", name: t`Uganda` },
        { code: "ZM", name: t`Zambia` },
        { code: "ZW", name: t`Zimbabwe` }
      ]
    },
    {
      name: t`Oceania`,
      countries: [
        { code: "AU", name: t`Australia` },
        { code: "FJ", name: t`Fiji` },
        { code: "KI", name: t`Kiribati` },
        { code: "MH", name: t`Marshall Islands` },
        { code: "FM", name: t`Micronesia` },
        { code: "NR", name: t`Nauru` },
        { code: "NZ", name: t`New Zealand` },
        { code: "PW", name: t`Palau` },
        { code: "PG", name: t`Papua New Guinea` },
        { code: "WS", name: t`Samoa` },
        { code: "SB", name: t`Solomon Islands` },
        { code: "TO", name: t`Tonga` },
        { code: "TV", name: t`Tuvalu` },
        { code: "VU", name: t`Vanuatu` }
      ]
    }
  ];

  const handleAddressChange = (newAddress: AddressData) => {
    setAddress(newAddress);
  };

  const handleAddressSelect = (selectedAddress: AddressData) => {
    setAddress(selectedAddress);
    setSelectedCountry(selectedAddress.country);
  };

  const handleFormSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);

    updateCurrentTenantMutation.mutate({
      body: {
        name: formData.get("name") as string,
        street: address.street || null,
        city: address.city || null,
        zip: address.zip || null,
        state: address.state || null,
        country: selectedCountry || null
      }
    });
  };

  if (isLoading) {
    return null;
  }

  return (
    <>
      <div className="flex h-full w-full gap-4">
        <SharedSideMenu ariaLabel={t`Toggle collapsed menu`} />
        <div className="flex w-full flex-col gap-4 px-4 py-3">
          <TopMenu>
            <Breadcrumb href="/admin/account">
              <Trans>Account</Trans>
            </Breadcrumb>
            <Breadcrumb>
              <Trans>Settings</Trans>
            </Breadcrumb>
          </TopMenu>
          <div className="20 mb-4 flex w-full items-center justify-between space-x-2 sm:mt-4">
            <div className="mt-3 flex flex-col gap-2 font-semibold text-3xl text-foreground">
              <h1>
                <Trans>Account settings</Trans>
              </h1>
              <p className="font-normal text-muted-foreground text-sm">
                <Trans>Manage your account here.</Trans>
              </p>
            </div>
          </div>

          <Form
            onSubmit={handleFormSubmit}
            validationErrors={updateCurrentTenantMutation.error?.errors}
            validationBehavior="aria"
            className="flex flex-col gap-4"
          >
            <Label>
              <Trans>Logo</Trans>
            </Label>
            <img src={logoWrap} alt={t`Logo`} className="max-h-16 max-w-64" />

            <div className="flex flex-col gap-4">
              <div className="flex flex-col gap-2">
                <h3 className="font-semibold text-lg">
                  <Trans>Address</Trans>
                </h3>
              </div>
              <Separator />
              <p className="font-normal text-muted-foreground text-sm">
                <Trans>Enter name and address information.</Trans>
              </p>
            </div>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div className="w-full">
                <TextField
                  autoFocus={true}
                  isRequired={true}
                  name="name"
                  defaultValue={tenant?.name ?? ""}
                  isDisabled={updateCurrentTenantMutation.isPending}
                  label={t`Name`}
                  placeholder={t`Enter name`}
                  validationBehavior="aria"
                />
              </div>

              <div className="w-full">
                <Select
                  label={t`Country`}
                  name="country"
                  selectedKey={selectedCountry}
                  onSelectionChange={(value) => {
                    setSelectedCountry(value as string);
                    setAddress((prev) => ({ ...prev, country: value as string }));
                  }}
                  isDisabled={updateCurrentTenantMutation.isPending}
                  placeholder={t`Select country`}
                >
                  {continents.map((continent) => (
                    <Section key={continent.name}>
                      <Header className="sticky top-0 z-10 border-b bg-background px-2 py-1 font-semibold text-muted-foreground text-xs">
                        {continent.name}
                      </Header>
                      {continent.countries.map((country) => (
                        <SelectItem key={country.code} id={country.code}>
                          {country.name}
                        </SelectItem>
                      ))}
                    </Section>
                  ))}
                </Select>
              </div>
            </div>

            <AddressForm
              address={address}
              onAddressChange={handleAddressChange}
              onAddressSelect={handleAddressSelect}
              countryCode={selectedCountry}
              isDisabled={updateCurrentTenantMutation.isPending || !selectedCountry}
            />

            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div className="w-full">
                <TextField
                  label={t`State/Province`}
                  name="state"
                  value={address.state}
                  onChange={(value) => {
                    setAddress((prev) => ({ ...prev, state: value as string }));
                  }}
                  isDisabled={updateCurrentTenantMutation.isPending || !selectedCountry}
                  placeholder={t`Enter state or province`}
                />
              </div>
            </div>

            <FormErrorMessage error={updateCurrentTenantMutation.error} />
            <Button type="submit" className="mt-4" isDisabled={updateCurrentTenantMutation.isPending}>
              {updateCurrentTenantMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
            </Button>
          </Form>

          <div className="mt-6 flex flex-col gap-4">
            <h3 className="font-semibold">
              <Trans>Danger zone</Trans>
            </h3>
            <Separator />
            <div className="flex flex-col gap-4">
              <p className="font-normal text-sm">
                <Trans>
                  Deleting the account and all associated data. This action cannot be undone, so please proceed with
                  caution.
                </Trans>
              </p>

              <Button variant="destructive" onPress={() => setIsDeleteModalOpen(true)} className="w-fit">
                <Trash2 />
                <Trans>Delete account</Trans>
              </Button>
            </div>
          </div>

          <Separator className="my-8" />
        </div>
      </div>

      <DeleteAccountConfirmation isOpen={isDeleteModalOpen} onOpenChange={setIsDeleteModalOpen} />
    </>
  );
}
