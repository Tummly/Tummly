import { Check, X } from "lucide-react"

import { GuestFeedbackMicIcon } from "@/components/guest-feedback/GuestFeedbackMicIcon"
import { GuestFeedbackWaveform } from "@/components/guest-feedback/GuestFeedbackWaveform"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import type { GuestMicChrome } from "@/lib/guestFeedback/createGuestMicSttModule"
import type { GuestMicAudioLevelSource } from "@/lib/guestFeedback/guestMicAudioLevel"
import { cn } from "@/lib/utils"

type AiAssistantMicChromeProps = {
  chrome: GuestMicChrome
  micAvailable: boolean
  micLocked: boolean
  levelSource: GuestMicAudioLevelSource
  onStart: () => void
  onConfirm: () => void
  onCancel: () => void
}

const CIRCLE_CLASS =
  "size-10 min-h-11 min-w-11 shrink-0 rounded-full bg-[var(--op-color-gray-1000)] p-2 text-[var(--op-color-gray-550)] shadow-none hover:bg-[var(--op-color-gray-1000)] hover:text-[var(--op-color-gray-550)] md:min-h-10 md:min-w-10"

export function AiAssistantMicChrome({
  chrome,
  micAvailable,
  micLocked,
  levelSource,
  onStart,
  onConfirm,
  onCancel,
}: AiAssistantMicChromeProps) {
  if (chrome === "tick_cancel") {
    return (
      <div className="flex h-10 w-full items-center gap-3">
        <Button
          type="button"
          variant="op-ghost"
          size="icon"
          aria-label="Cancel recording"
          onClick={onCancel}
          className={CIRCLE_CLASS}
        >
          <X className="size-5" strokeWidth={2} />
        </Button>
        <GuestFeedbackWaveform
          levelSource={levelSource}
          barClassName="bg-[var(--op-color-gray-550)]"
        />
        <Button
          type="button"
          variant="op-ghost"
          size="icon"
          aria-label="Stop recording and transcribe"
          onClick={onConfirm}
          className={CIRCLE_CLASS}
        >
          <Check className="size-5" strokeWidth={2} />
        </Button>
      </div>
    )
  }

  if (chrome === "loader") {
    return (
      <div
        className={cn(CIRCLE_CLASS, "flex items-center justify-center")}
        role="status"
        aria-label="Transcribing"
      >
        <Spinner size="sm" className="border-[var(--op-color-gray-750)] border-t-[var(--op-color-gray-550)]" />
      </div>
    )
  }

  return (
    <Button
      type="button"
      variant="op-ghost"
      size="icon"
      aria-label="Dictate question"
      disabled={micLocked || !micAvailable}
      onClick={onStart}
      className={CIRCLE_CLASS}
    >
      <GuestFeedbackMicIcon />
    </Button>
  )
}
