import { Button, FieldError, Form, Input, Label, TextField } from "react-aria-components";
import { useFormState } from "react-dom";
import { createTenant, State } from "./actions";
import { Trans } from "@lingui/macro";
import { useLingui } from "@lingui/react";

export default function CreateTenantForm() {
  const initialState: State = { message: null, errors: {} };
  const [state, formAction] = useFormState(createTenant, initialState);
  const { i18n } = useLingui();

  return (
    <Form
      action={formAction}
      validationErrors={state.errors}
      className="w-full h-full flex flex-col p-2 justify-center items-center border-border border"
    >
      <div className="flex flex-col w-fit bg-gray-200 rounded p-4 gap-2 shadow-sm">
        <h1 className="text-xl font-bold">
          <Trans>Create a account</Trans>
        </h1>
        <TextField name="subdomain" autoFocus className="flex flex-col" isRequired>
          <Label>
            <Trans>Subdomain</Trans>
          </Label>
          <Input className="p-2 rounded-md border border-black" placeholder={i18n.t("subdomain")} />
          <FieldError />
        </TextField>

        <TextField name="name" type="username" className="flex flex-col" isRequired>
          <Label>
            <Trans>Name</Trans>
          </Label>
          <Input className="p-2 rounded-md border border-black" placeholder={i18n.t("name")} />
          <FieldError />
        </TextField>

        <TextField name="email" type="email" className="flex flex-col" isRequired>
          <Label>
            <Trans>Email</Trans>
          </Label>
          <Input className="p-2 rounded-md border border-black" placeholder={i18n.t("email")} />
          <FieldError />
        </TextField>
        <Button
          type="submit"
          className="bg-blue-600 p-2 rounded-md text-white text-sm border border-border shadow-lg hover:bg-slate-400 w-fit"
        >
          <Trans>Create account!</Trans>
        </Button>
      </div>
    </Form>
  );
}
