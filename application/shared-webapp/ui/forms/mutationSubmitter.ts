import type { FormEvent } from "react";

type MutationParams = {
  body?: unknown;
  params?: {
    path?: Record<string, string>;
    query?: Record<string, string>;
  };
};

export function mutationSubmitter<T extends MutationParams>(
  mutation: { mutate: (data: T) => void },
  params?: T["params"]
) {
  return (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.target as HTMLFormElement);
    const body = Object.fromEntries(formData);

    const mutationData = {
      ...(Object.keys(body).length > 0 && { body }),
      params
    } as T;

    mutation.mutate(mutationData);
  };
}
