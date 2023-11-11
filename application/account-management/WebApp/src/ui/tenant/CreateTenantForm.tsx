import { Button, FieldError, Form, Input, Label, TextField } from "react-aria-components";
import { useFormState } from "react-dom";
import { createTenant, State } from "./actions";

export function CreateTenantForm() {
  const initialState: State = { message: null, errors: {} };
  const [state, formAction] = useFormState(createTenant, initialState);

  return (
    <Form
      action={formAction}
      validationErrors={state.errors}
      className="w-screen h-screen bg-slate-900 flex flex-col p-2 justify-center items-center"
    >
      <div className="flex flex-col w-fit bg-slate-300 rounded-sm p-4 gap-2">
        <h1 className="text-xl font-bold">Create a tenant</h1>
        <TextField name={"subdomain"} autoFocus className={"flex flex-col"} isRequired>
          <Label>Subdomain</Label>
          <Input className="p-2 rounded-md border border-black" placeholder="subdomain" />
          <FieldError />
        </TextField>

        <TextField name={"name"} type={"username"} className={"flex flex-col"} isRequired>
          <Label>Name</Label>
          <Input className="p-2 rounded-md border border-black" placeholder="name" />
          <FieldError />
        </TextField>

        <TextField name={"email"} type={"email"} className={"flex flex-col"} isRequired>
          <Label>Email</Label>
          <Input className="p-2 rounded-md border border-black" placeholder="email" />
          <FieldError />
        </TextField>

        <Button
          type="submit"
          className="bg-slate-500 p-2 rounded-md text-white text-sm border border-black hover:bg-slate-400 w-fit"
        >
          Create tenant!
        </Button>
      </div>
    </Form>
  );
}
