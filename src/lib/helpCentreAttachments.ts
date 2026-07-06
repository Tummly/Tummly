export const HELP_CENTRE_ATTACHMENT_MAX_FILES = 5

export const HELP_CENTRE_ATTACHMENT_MAX_FILE_BYTES = 10 * 1024 * 1024

export const HELP_CENTRE_ATTACHMENT_MAX_TOTAL_BYTES = 50 * 1024 * 1024

export const HELP_CENTRE_ATTACHMENT_ACCEPT =
  "image/jpeg,image/png,image/webp,image/gif,application/pdf,.jpg,.jpeg,.png,.webp,.gif,.pdf"

export function formatHelpCentreAttachmentSize(bytes: number) {
  if (bytes < 1024) {
    return `${bytes} B`
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function validateHelpCentreAttachments(files: File[]): string | null {
  if (files.length === 0) {
    return null
  }

  if (files.length > HELP_CENTRE_ATTACHMENT_MAX_FILES) {
    return `You can attach up to ${HELP_CENTRE_ATTACHMENT_MAX_FILES} files.`
  }

  let totalBytes = 0

  for (const file of files) {
    if (file.size <= 0) {
      return "One or more attachments are empty."
    }

    if (file.size > HELP_CENTRE_ATTACHMENT_MAX_FILE_BYTES) {
      return "Each attachment must be 10 MB or smaller."
    }

    totalBytes += file.size

    const extension = file.name.includes(".")
      ? file.name.slice(file.name.lastIndexOf(".")).toLowerCase()
      : ""

    const allowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf"]
    const allowedTypes = [
      "image/jpeg",
      "image/png",
      "image/webp",
      "image/gif",
      "application/pdf",
    ]

    if (!allowedExtensions.includes(extension)) {
      return "Attachments must be JPEG, PNG, WebP, GIF, or PDF files."
    }

    if (file.type && !allowedTypes.includes(file.type)) {
      return "Attachments must be JPEG, PNG, WebP, GIF, or PDF files."
    }
  }

  if (totalBytes > HELP_CENTRE_ATTACHMENT_MAX_TOTAL_BYTES) {
    return "Total attachment size must be 50 MB or smaller."
  }

  return null
}
