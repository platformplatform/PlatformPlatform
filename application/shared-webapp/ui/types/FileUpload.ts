/**
 * Type-safe wrapper for file uploads to handle the disconnect between
 * OpenAPI generated types (which show string) and runtime requirements (which need FormData)
 *
 * @see https://github.com/openapi-ts/openapi-typescript/issues/1214
 */
export type FileUploadMutation = {
  mutateAsync: (params: { body: FormData }) => Promise<unknown>;
};
