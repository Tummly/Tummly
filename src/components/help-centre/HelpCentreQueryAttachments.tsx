import { useState } from "react"
import { DownloadIcon } from "lucide-react"

import { formatHelpCentreAttachmentSize } from "@/lib/helpCentreAttachments"
import type { HelpCentreQueryAttachment } from "@/types/helpCentre"

type HelpCentreQueryAttachmentsProps = {
  queryId: number
  attachments: HelpCentreQueryAttachment[]
  onDownload: (queryId: number, attachmentId: number) => Promise<Blob>
}

export function HelpCentreQueryAttachments({
  queryId,
  attachments,
  onDownload,
}: HelpCentreQueryAttachmentsProps) {
  const [downloadingId, setDownloadingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  if (attachments.length === 0) {
    return null
  }

  const handleDownload = async (attachment: HelpCentreQueryAttachment) => {
    setError(null)
    setDownloadingId(attachment.id)

    try {
      const blob = await onDownload(queryId, attachment.id)
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement("a")
      anchor.href = url
      anchor.download = attachment.fileName
      anchor.click()
      URL.revokeObjectURL(url)
    } catch {
      setError("Unable to download this attachment.")
    } finally {
      setDownloadingId(null)
    }
  }

  return (
    <section className="flex flex-col gap-3">
      <h2 className="m-0 text-sm font-semibold text-[#232323]">Attachments</h2>
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {attachments.map((attachment) => (
          <li
            key={attachment.id}
            className="flex items-center justify-between gap-3 rounded-xl border border-border bg-white px-4 py-3"
          >
            <div className="min-w-0 flex-1">
              <p className="m-0 truncate text-sm font-medium text-[#232323]">
                {attachment.fileName}
              </p>
              <p className="m-0 text-xs text-muted-foreground">
                {formatHelpCentreAttachmentSize(attachment.sizeBytes)}
              </p>
            </div>
            <button
              type="button"
              onClick={() => void handleDownload(attachment)}
              disabled={downloadingId === attachment.id}
              className="inline-flex shrink-0 items-center gap-1.5 text-sm font-medium text-[#14a74a] underline-offset-2 hover:underline disabled:opacity-50"
            >
              <DownloadIcon className="size-4" aria-hidden />
              {downloadingId === attachment.id ? "Downloading..." : "Download"}
            </button>
          </li>
        ))}
      </ul>
      {error && (
        <p className="m-0 text-sm text-destructive" role="alert">
          {error}
        </p>
      )}
    </section>
  )
}
