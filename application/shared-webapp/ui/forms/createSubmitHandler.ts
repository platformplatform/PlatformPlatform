import type { FormEvent } from "react";

export function createSubmitHandler<TBody>(mutate: (body: TBody) => void) {
  return (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.target as HTMLFormElement);
    // biome-ignore lint/suspicious/noExplicitAny: Same any-cast as we did in PlatformServerAction.ts
    mutate({ body: Object.fromEntries(formData) } as any);
  };
}
