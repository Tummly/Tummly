import { Check, Loader2, Mic, X } from "lucide-react"

import { Button } from "@/components/ui/button"
import type { GuestMicChrome } from "@/lib/guestFeedback/createGuestMicSttModule"
import { cn } from "@/lib/utils"

type GuestFeedbackMicChromeProps = {
  chrome: GuestMicChrome
  micAvailable: boolean
  disabled?: boolean
  onStart: () => void
  onConfirm: () => void
  onCancel: () => void
}

export function GuestFeedbackMicChrome({
  chrome,
  micAvailable,
  disabled = false,
  onStart,
  onConfirm,
  onCancel,
}: GuestFeedbackMicChromeProps) {
  if (chrome === "tick_cancel") {
    return (
      <div className="flex items-center gap-2">
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label="Cancel recording"
          onClick={onCancel}
          className="size-8 rounded-full text-guest-feedback-muted hover:bg-white/5 hover:text-guest-feedback-text"
        >
          <X className="size-5" />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label="Stop recording and transcribe"
          onClick={onConfirm}
          className="size-8 rounded-full text-guest-feedback-accent hover:bg-guest-feedback-accent/10 hover:text-guest-feedback-accent"
        >
          <Check className="size-5" />
        </Button>
      </div>
    )
  }

  if (chrome === "loader") {
    return (
      <div
        className="flex size-8 items-center justify-center text-guest-feedback-muted"
        role="status"
        aria-label="Transcribing"
      >
        <Loader2 className="size-5 animate-spin" />
      </div>
    )
  }

  return (
    <Button
      type="button"
      variant="ghost"
      size="icon-sm"
      aria-label="Dictate feedback"
      disabled={disabled || !micAvailable}
      onClick={onStart}
      className={cn(
        "size-8 rounded-full text-guest-feedback-placeholder hover:bg-white/5 hover:text-guest-feedback-text",
        !micAvailable && "opacity-40"
      )}
    >
      <Mic className="size-5" />
    </Button>
  )
}
