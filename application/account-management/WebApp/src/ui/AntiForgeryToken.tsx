/**
 * AntiForgeryToken Hidden Form Element
 */
export function AntiForgeryToken() {
  return (
    <input type="hidden" name="__RequestVerificationToken" value={import.meta.env.XSRF_TOKEN} />
  );
}
