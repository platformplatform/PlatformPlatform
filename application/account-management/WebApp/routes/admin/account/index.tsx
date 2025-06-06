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
import { TextField } from "@repo/ui/components/TextField";
import { createFileRoute } from "@tanstack/react-router";
import { Trash2 } from "lucide-react";
import type React from "react";
import { useEffect, useState } from "react";
import { Label, Separator } from "react-aria-components";
import DeleteAccountConfirmation from "./-components/DeleteAccountConfirmation";

export const Route = createFileRoute("/admin/account/")({
  component: AccountSettings
});

export function AccountSettings() {
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
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
      setAddress({
        street: tenant.street ?? "",
        street2: "",
        city: tenant.city ?? "",
        state: tenant.addressState ?? "",
        zip: tenant.zip ?? "",
        country: tenant.country ?? ""
      });
    }
  }, [tenant]);

  const handleAddressChange = (newAddress: AddressData) => {
    setAddress(newAddress);
  };

  const handleAddressSelect = (selectedAddress: AddressData) => {
    setAddress(selectedAddress);
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
        country: address.country || null
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
            </div>

            <AddressForm
              address={address}
              onAddressChange={handleAddressChange}
              onAddressSelect={handleAddressSelect}
              isDisabled={updateCurrentTenantMutation.isPending}
            />

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
