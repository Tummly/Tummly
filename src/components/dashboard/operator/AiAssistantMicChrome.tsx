import { Check, X } from "lucide-react"

import { GuestFeedbackMicIcon } from "@/components/guest-feedback/GuestFeedbackMicIcon"
import { GuestFeedbackWaveform } from "@/components/guest-feedback/GuestFeedbackWaveform"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { ASSISTANT_COMPOSER_CIRCLE_CLASS } from "@/lib/operatorAiAssistant/assistantCreditsPresentation"
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
          className={ASSISTANT_COMPOSER_CIRCLE_CLASS}
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
          className={ASSISTANT_COMPOSER_CIRCLE_CLASS}
        >
          <Check className="size-5" strokeWidth={2} />
        </Button>
      </div>
    )
  }

  if (chrome === "loader") {
    return (
      <div
        className={cn(ASSISTANT_COMPOSER_CIRCLE_CLASS, "flex items-center justify-center")}
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
      className={ASSISTANT_COMPOSER_CIRCLE_CLASS}
    >
      <GuestFeedbackMicIcon />
    </Button>
  )
}
