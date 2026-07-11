import { useState } from "react"
import { DownloadIcon } from "lucide-react"

import { downloadSupportQueryAttachment } from "@/api/supportApi"
import { formatHelpCentreAttachmentSize } from "@/lib/helpCentreAttachments"
import type { SupportQueryDetail } from "@/types/support"

export function SupportQueryAttachments({
  queryId,
  attachments,
}: {
  queryId: number
  attachments: NonNullable<SupportQueryDetail["attachments"]>
}) {
  const [downloadingId, setDownloadingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleDownload = async (attachmentId: number, fileName: string) => {
    setError(null)
    setDownloadingId(attachmentId)

    try {
      const blob = await downloadSupportQueryAttachment(queryId, attachmentId)
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement("a")
      anchor.href = url
      anchor.download = fileName
      anchor.click()
      URL.revokeObjectURL(url)
    } catch {
      setError("Unable to download this attachment.")
    } finally {
      setDownloadingId(null)
    }
  }

  if (attachments.length === 0) {
    return null
  }

  return (
    <section className="flex flex-col gap-3">
      <h3 className="text-sm font-semibold">Attachments</h3>
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {attachments.map((attachment) => (
          <li
            key={attachment.id}
            className="flex items-center justify-between gap-3 rounded-lg border border-border bg-muted/20 px-4 py-3"
          >
            <div className="min-w-0 flex-1">
              <p className="m-0 truncate text-sm font-medium">
                {attachment.fileName}
              </p>
              <p className="m-0 text-xs text-muted-foreground">
                {formatHelpCentreAttachmentSize(attachment.sizeBytes)}
              </p>
            </div>
            <button
              type="button"
              onClick={() =>
                void handleDownload(attachment.id, attachment.fileName)
              }
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
