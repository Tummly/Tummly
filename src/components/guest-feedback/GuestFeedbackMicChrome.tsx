import { Check, Loader2, Mic, X } from "lucide-react"

import { GuestFeedbackWaveform } from "@/components/guest-feedback/GuestFeedbackWaveform"
import { Button } from "@/components/ui/button"
import type { GuestMicChrome } from "@/lib/guestFeedback/createGuestMicSttModule"
import type { GuestMicAudioLevelSource } from "@/lib/guestFeedback/guestMicAudioLevel"
import { cn } from "@/lib/utils"

type GuestFeedbackMicChromeProps = {
  chrome: GuestMicChrome
  micAvailable: boolean
  levelSource: GuestMicAudioLevelSource
  disabled?: boolean
  onStart: () => void
  onConfirm: () => void
  onCancel: () => void
}

const controlClassName =
  "size-10 shrink-0 rounded-full border border-white/15 p-0 text-guest-feedback-text shadow-none hover:bg-white/10 hover:text-white"

/**
 * Comment-box mic chrome (Guest-Loop-MVP nodes 3216:26395 / 3216:26436):
 * idle shows a bordered-circle mic in the bottom-right; recording expands to
 * a full-width strip — cancel X left, live waveform middle, confirm tick
 * right (same bordered-circle style as the X); transcribing keeps the spinner.
 */
export function GuestFeedbackMicChrome({
  chrome,
  micAvailable,
  levelSource,
  disabled = false,
  onStart,
  onConfirm,
  onCancel,
}: GuestFeedbackMicChromeProps) {
  if (chrome === "tick_cancel") {
    return (
      <div className="flex h-10 w-full items-center gap-3">
        <Button
          type="button"
          variant="outline-inverse"
          size="icon-lg"
          aria-label="Cancel recording"
          onClick={onCancel}
          className={cn(controlClassName, "text-guest-feedback-muted")}
        >
          <X className="size-5" strokeWidth={2} />
        </Button>
        <GuestFeedbackWaveform levelSource={levelSource} />
        <Button
          type="button"
          variant="outline-inverse"
          size="icon-lg"
          aria-label="Stop recording and transcribe"
          onClick={onConfirm}
          className={cn(controlClassName, "text-guest-feedback-muted")}
        >
          <Check className="size-5" strokeWidth={2} />
        </Button>
      </div>
    )
  }

  if (chrome === "loader") {
    return (
      <div
        className="flex size-10 items-center justify-center text-guest-feedback-muted"
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
      variant="outline-inverse"
      size="icon-lg"
      aria-label="Dictate feedback"
      disabled={disabled || !micAvailable}
      onClick={onStart}
      className={cn(controlClassName, !micAvailable && "opacity-40")}
    >
      <Mic className="size-5" />
    </Button>
  )
}
