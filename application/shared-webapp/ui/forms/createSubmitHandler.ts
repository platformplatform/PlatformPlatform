import type { FormEvent } from "react";

export function createSubmitHandler<TBody>(mutate: (body: TBody) => void) {
  return (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.target as HTMLFormElement);
    mutate({ body: Object.fromEntries(formData) } as TBody);
  };
}
