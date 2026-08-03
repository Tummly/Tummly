import { Button } from "@/components/ui/button"
import {
  GUEST_PREVIEW_CONTROL_LABEL,
  GUEST_PREVIEW_EDIT_TEXT_LABEL,
  GUEST_PREVIEW_HEADING,
  guestPreviewMockTitle,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"

type GuestPreviewPanelProps = {
  channel: RespondToGuestChannel | null
  subject: string
  message: string
  disabled?: boolean
  onEditText: () => void
}

/**
 * Review right-rail Guest preview — read-only email/SMS mock.
 * Edit text returns to Guest response; Send test is out of scope (PRD fog).
 */
export function GuestPreviewPanel({
  channel,
  subject,
  message,
  disabled = false,
  onEditText,
}: GuestPreviewPanelProps) {
  const isSms = channel === "sms"
  const mockTitle = guestPreviewMockTitle(channel)

  return (
    <aside className="flex w-full flex-1 flex-col gap-2.5">
      <h2 className="text-base font-semibold text-op-text-primary">
        {GUEST_PREVIEW_HEADING}
      </h2>

      <div className="flex min-h-[180px] w-full flex-col overflow-clip rounded-[4px] bg-[var(--op-color-gray-990)]">
        <div
          className="relative flex min-h-[180px] flex-1 flex-col justify-between gap-4 p-6"
          style={{
            backgroundImage:
              "linear-gradient(90deg, rgba(20, 20, 20, 0.55) 0%, rgba(20, 20, 20, 0.55) 100%), linear-gradient(180deg, rgba(27, 27, 27, 0) 26%, rgb(27, 27, 27) 88%)",
          }}
        >
          <p className="text-center text-base font-semibold text-op-text-primary">
            {mockTitle}
          </p>

          <div
            className="pointer-events-none absolute inset-0 flex items-center justify-center px-6"
            aria-hidden
          >
            <div className="max-h-[70%] w-full overflow-hidden rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] p-4 opacity-40">
              {isSms ? (
                <p className="whitespace-pre-wrap text-sm text-op-text-primary">
                  {message.trim() || "—"}
                </p>
              ) : (
                <div className="flex flex-col gap-2">
                  {subject.trim() ? (
                    <p className="text-sm font-semibold text-op-text-primary">
                      {subject}
                    </p>
                  ) : null}
                  <p className="whitespace-pre-wrap text-sm text-op-text-primary">
                    {message.trim() || "—"}
                  </p>
                </div>
              )}
            </div>
          </div>

          <div className="relative z-10 flex justify-center">
            <span className="inline-flex items-center rounded-[2px] bg-op-button-secondary-background px-4 py-2 text-xs font-semibold text-op-text-primary">
              {GUEST_PREVIEW_CONTROL_LABEL}
            </span>
          </div>

          <div className="relative z-10 flex flex-wrap gap-3">
            <Button
              type="button"
              variant="op-tertiary"
              size="sm"
              disabled={disabled}
              onClick={onEditText}
            >
              {GUEST_PREVIEW_EDIT_TEXT_LABEL}
            </Button>
          </div>
        </div>
      </div>
    </aside>
  )
}
