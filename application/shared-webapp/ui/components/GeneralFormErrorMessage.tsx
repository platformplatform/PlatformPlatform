import { FormErrorMessage } from "./FormErrorMessage";

type GeneralFormErrorMessageProps = { error: { title: string | null; detail: string | null } | null };

export function GeneralFormErrorMessage({ error }: Readonly<GeneralFormErrorMessageProps>) {
  if (!error) return null;
  if (!error.title && !error.detail) return null;
  return <FormErrorMessage title={error.title ?? "undefined"} message={error.detail ?? undefined} />;
}
