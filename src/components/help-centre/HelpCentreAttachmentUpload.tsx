import { useRef } from "react"
import { UploadIcon, XIcon } from "lucide-react"

import {
  formatHelpCentreAttachmentSize,
  HELP_CENTRE_ATTACHMENT_ACCEPT,
  HELP_CENTRE_ATTACHMENT_MAX_FILES,
  validateHelpCentreAttachments,
} from "@/lib/helpCentreAttachments"
import { cn } from "@/lib/utils"

type HelpCentreAttachmentUploadProps = {
  files: File[]
  onChange: (files: File[]) => void
  error?: string | null
  className?: string
}

export function HelpCentreAttachmentUpload({
  files,
  onChange,
  error,
  className,
}: HelpCentreAttachmentUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)

  const handleFilesSelected = (selected: FileList | null) => {
    if (!selected?.length) {
      return
    }

    const next = [...files, ...Array.from(selected)].slice(
      0,
      HELP_CENTRE_ATTACHMENT_MAX_FILES
    )
    onChange(next)

    if (inputRef.current) {
      inputRef.current.value = ""
    }
  }

  const validationError = validateHelpCentreAttachments(files)
  const displayError = error ?? validationError

  return (
    <div className={cn("flex w-full flex-col gap-5", className)}>
      <div className="flex flex-col gap-1">
        <h2 className="m-0 text-xl font-bold text-[#141414]">Attachments</h2>
        <p className="m-0 text-sm font-medium text-[#605e5c]">
          Add screenshots, exports, or files that will help us understand the
          request.
        </p>
      </div>

      <div className="flex flex-col gap-3">
        <input
          ref={inputRef}
          type="file"
          multiple
          accept={HELP_CENTRE_ATTACHMENT_ACCEPT}
          className="sr-only"
          onChange={(event) => handleFilesSelected(event.target.files)}
        />
        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          disabled={files.length >= HELP_CENTRE_ATTACHMENT_MAX_FILES}
          className="inline-flex w-fit items-center gap-3 rounded-[32px] bg-[#eaeaea] px-4 py-3 text-sm text-[#141414] transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
        >
          <UploadIcon className="size-4 shrink-0" aria-hidden />
          Upload files
        </button>

        {files.length > 0 && (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {files.map((file, index) => (
              <li
                key={`${file.name}-${file.size}-${index}`}
                className="flex items-center justify-between gap-3 rounded-md border border-border bg-white px-3 py-2"
              >
                <div className="min-w-0 flex-1">
                  <p className="m-0 truncate text-sm font-medium text-[#141414]">
                    {file.name}
                  </p>
                  <p className="m-0 text-xs text-muted-foreground">
                    {formatHelpCentreAttachmentSize(file.size)}
                  </p>
                </div>
                <button
                  type="button"
                  aria-label={`Remove ${file.name}`}
                  onClick={() =>
                    onChange(files.filter((_, fileIndex) => fileIndex !== index))
                  }
                  className="shrink-0 rounded-sm p-1 text-muted-foreground hover:text-foreground"
                >
                  <XIcon className="size-4" aria-hidden />
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {displayError && (
        <p className="m-0 text-sm text-destructive" role="alert">
          {displayError}
        </p>
      )}
    </div>
  )
}
