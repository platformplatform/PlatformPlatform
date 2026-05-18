export const MAX_FILE_SIZE = 5 * 1024 * 1024;

export const ACCEPTED_IMAGE_TYPES = {
  "image/jpeg": [".jpg", ".jpeg"],
  "image/png": [".png"],
  "image/gif": [".gif"],
  "image/webp": [".webp"]
};

export interface UploadedImage {
  file: File;
  previewUrl: string;
}
