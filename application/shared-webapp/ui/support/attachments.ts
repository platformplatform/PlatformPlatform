// Shared support-attachment helpers used by both the end-user support UI (account WebApp) and the
// back-office support inbox (account BackOffice). These are pure, locale-independent constraints and
// formatting, so they live in the shared UI package rather than being duplicated per surface. The
// enum-keyed display maps (status/category/CSAT labels and palettes) intentionally stay per surface:
// they are keyed by each app's generated SupportTicket* enums, and the user- vs staff-facing copy and
// colours legitimately differ.

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  const kb = bytes / 1024;
  if (kb < 1024) {
    return `${kb.toFixed(kb < 10 ? 1 : 0)} KB`;
  }
  const mb = kb / 1024;
  return `${mb.toFixed(mb < 10 ? 1 : 0)} MB`;
}

export const MAX_ATTACHMENT_BYTES = 25 * 1024 * 1024;
export const MAX_ATTACHMENTS = 5;
export const ALLOWED_ATTACHMENT_EXTENSIONS = [
  ".jpg",
  ".jpeg",
  ".png",
  ".gif",
  ".webp",
  ".pdf",
  ".txt",
  ".csv",
  ".log",
  ".zip"
];
