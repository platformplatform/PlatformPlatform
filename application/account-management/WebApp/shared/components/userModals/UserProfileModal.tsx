import { useCallback, useEffect, useState } from "react";
import { useFormState } from "react-dom";
import { FileTrigger, Form, Heading, Label } from "react-aria-components";
import { XIcon } from "lucide-react";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { Modal } from "@repo/ui/components/Modal";
import { TextField } from "@repo/ui/components/TextField";
import type { Schemas } from "@/shared/lib/api/client";
import { api } from "@/shared/lib/api/client";

type ProfileModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  userId: string;
};

export default function UserProfileModal({ isOpen, onOpenChange, userId }: Readonly<ProfileModalProps>) {
  const [data, setData] = useState<Schemas["UserResponseDto"] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [file, setFile] = useState<string | null>(null);

  const onFileSelect = (e: FileList | null) => {
    if (e) {
      setFile(Array.from(e)[0].name);
    }
  };

  const closeDialog = useCallback(() => {
    onOpenChange(false);
  }, [onOpenChange]);

  useEffect(() => {
    (async () => {
      if (isOpen) {
        setLoading(true);
        setData(null);
        setError(null);

        try {
          const response = await api.get("/api/account-management/users/{id}", { params: { path: { id: userId } } });
          setData(response);
        } catch (error) {
          // biome-ignore lint/suspicious/noExplicitAny: We don't know the type at this point
          setError(error as any);
        } finally {
          setLoading(false);
        }
      }
    })();
  }, [isOpen, userId]);

  let [{ success, errors, title, message }, action, isPending] = useFormState(
    api.actionPut("/api/account-management/users/{id}"),
    {
      success: null
    }
  );

  useEffect(() => {
    if (isPending) {
      success = undefined;
    }

    if (success) {
      closeDialog();
    }
  }, [success, isPending, closeDialog]);

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={!loading}>
      <Dialog>
        {!data && (
          <Heading slot="title">
            {loading && "Fetching data..."}
            {error && JSON.stringify(error)}
          </Heading>
        )}

        {data && (
          <>
            <XIcon onClick={closeDialog} className="h-10 w-10 absolute top-2 right-2 p-2 hover:bg-muted" />
            <Heading slot="title" className="text-2xl">
              User profile
            </Heading>
            <p className="text-muted-foreground text-sm">Update photo and personal details here.</p>

            <Form
              action={action}
              validationErrors={errors}
              validationBehavior="aria"
              className="flex flex-col gap-4 mt-4"
            >
              <input type="hidden" name="id" value={userId} />
              <Label>Photo</Label>
              <FileTrigger onSelect={onFileSelect}>
                <Button variant="icon" className="rounded-full w-16 h-16 mb-3">
                  <img src={data.avatarUrl ?? ""} alt="User profile" className="rounded-full" />
                </Button>
              </FileTrigger>
              {file}

              <div className="flex flex-col sm:flex-row gap-4">
                <TextField
                  autoFocus
                  isRequired
                  name="firstName"
                  label="First name"
                  defaultValue={data.firstName}
                  placeholder="E.g. Olivia"
                  className="sm:w-64"
                />
                <TextField
                  isRequired
                  name="lastName"
                  label="Last name"
                  defaultValue={data.lastName}
                  placeholder="E.g. Rhye"
                  className="sm:w-64"
                />
              </div>
              <TextField name="email" label="Email" value={data?.email} />
              <TextField name="title" label="Title" defaultValue={data?.title} placeholder="E.g. Marketing Manager" />

              <FormErrorMessage title={title} message={message} />

              <div className="flex justify-end gap-4 mt-6">
                <Button type="reset" onPress={closeDialog} variant="secondary">
                  Cancel
                </Button>
                <Button type="submit" isDisabled={isPending}>
                  Save changes
                </Button>
              </div>
            </Form>
          </>
        )}
      </Dialog>
    </Modal>
  );
}
