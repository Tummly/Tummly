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

/**
 * Recording controls — Figma Guest-Loop-MVP 6829:13660.
 * Cancel and confirm share one 40px circular control style.
 */
const recordingControlClassName =
  "size-10 shrink-0 rounded-[70px] border border-[#1e1e1f] bg-[rgba(30,30,31,0.7)] p-2 text-[#f4f4f4] shadow-none hover:border-white/40 hover:bg-white/10 hover:text-white"

/**
 * Comment-box mic chrome — Figma Guest-Loop-MVP 6889:349 (idle Dictate) /
 * 6829:13608 (recording strip with cancel + confirm).
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
          className={recordingControlClassName}
        >
          <X className="size-3" strokeWidth={2} absoluteStrokeWidth />
        </Button>
        <GuestFeedbackWaveform levelSource={levelSource} />
        <Button
          type="button"
          variant="outline-inverse"
          size="icon-lg"
          aria-label="Stop recording and transcribe"
          onClick={onConfirm}
          className={recordingControlClassName}
        >
          <Check className="size-3.5" strokeWidth={2.5} absoluteStrokeWidth />
        </Button>
      </div>
    )
  }

  if (chrome === "loader") {
    return (
      <div
        className="flex h-12 w-full items-center justify-center text-guest-feedback-muted"
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
      aria-label="Dictate feedback"
      disabled={disabled || !micAvailable}
      onClick={onStart}
      className={cn(
        "h-auto min-h-12 w-full gap-0.5 rounded-[70px] border-0 bg-[rgba(30,30,31,0.5)] px-2 py-3 text-sm font-normal leading-normal text-guest-feedback-text shadow-none hover:bg-[rgba(30,30,31,0.7)] hover:text-white",
        !micAvailable && "opacity-40"
      )}
    >
      <Mic className="size-6 shrink-0" strokeWidth={1.75} aria-hidden />
      Dictate
    </Button>
  )
}
