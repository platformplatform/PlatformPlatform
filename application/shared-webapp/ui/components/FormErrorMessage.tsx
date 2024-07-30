import { Content, Heading, InlineAlert } from "./InlineAlert";

type FormErrorMessageProps = {
  title?: string;
  message?: string;
};

export function FormErrorMessage({ title, message }: FormErrorMessageProps) {
  if (title == null || message == null) return null;
  return (
    <InlineAlert variant="danger">
      <Heading>{title}</Heading>
      <Content>{message}</Content>
    </InlineAlert>
  );
}
