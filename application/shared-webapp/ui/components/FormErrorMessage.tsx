import { Content, Heading, InlineAlert } from "./InlineAlert";

type FormErrorMessageProps = { error: { title: string | null; detail: string | null } | null };

export function FormErrorMessage({ error }: Readonly<FormErrorMessageProps>) {
  if (!error) {
    return null;
  }
  if (!error.title || !error.detail) {
    return null;
  }

  return (
    <InlineAlert variant="danger">
      <Heading>{error.title}</Heading>
      <Content>{error.detail}</Content>
    </InlineAlert>
  );
}
