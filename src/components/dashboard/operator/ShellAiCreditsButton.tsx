import { useState } from "react"

import { Button } from "@/components/ui/button"
import {
  Popover,
  PopoverContent,
  PopoverTitle,
  PopoverTrigger,
} from "@/components/ui/popover"
import type { OperatorShellAiCreditsViewModel } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import {
  SHELL_AI_CREDITS_BODY_CLASS,
  SHELL_AI_CREDITS_FOOTER_CLASS,
  SHELL_AI_CREDITS_LEFT_LINE_CLASS,
  SHELL_AI_CREDITS_METER_FILL_CLASS,
  SHELL_AI_CREDITS_METER_TRACK_CLASS,
  SHELL_AI_CREDITS_POPOVER_CONTENT_CLASS,
  SHELL_AI_CREDITS_TITLE_CLASS,
  SHELL_AI_CREDITS_TRIGGER_CLASS,
  SHELL_AI_CREDITS_USED_LINE_CLASS,
} from "@/lib/operatorAiAssistant/shellAiCreditsPresentation"
import { GUESTS_PAGE_SECONDARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

type ShellAiCreditsButtonProps = {
  credits: OperatorShellAiCreditsViewModel
  onOpenChange?: (open: boolean) => void
  onViewUsage: () => void
  onAddCredits: () => void
}

/** Navbar AI credits trigger + usage popover — Figma 5216:26967. */
export function ShellAiCreditsButton({
  credits,
  onOpenChange,
  onViewUsage,
  onAddCredits,
}: ShellAiCreditsButtonProps) {
  const [open, setOpen] = useState(false)
  const fillPercent = Math.round(
    Math.min(1, Math.max(0, credits.fillRatio)) * 100
  )

  const handleOpenChange = (next: boolean) => {
    setOpen(next)
    onOpenChange?.(next)
  }

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="op-tertiary"
          size="op"
          className={SHELL_AI_CREDITS_TRIGGER_CLASS}
          aria-label={credits.buttonLabel}
        >
          {credits.buttonLabel}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        sideOffset={8}
        className={SHELL_AI_CREDITS_POPOVER_CONTENT_CLASS}
      >
        <PopoverTitle className="sr-only">{credits.title}</PopoverTitle>
        <div className={SHELL_AI_CREDITS_BODY_CLASS}>
          <p className={SHELL_AI_CREDITS_TITLE_CLASS}>{credits.title}</p>
          <div className="flex w-full flex-col gap-3">
            <p className={SHELL_AI_CREDITS_USED_LINE_CLASS}>{credits.usedLine}</p>
            <div className="flex w-full flex-col gap-2.5">
              <div
                className={SHELL_AI_CREDITS_METER_TRACK_CLASS}
                role="meter"
                aria-label={credits.usedLine}
                aria-valuemin={0}
                aria-valuemax={100}
                aria-valuenow={fillPercent}
              >
                <div
                  className={SHELL_AI_CREDITS_METER_FILL_CLASS}
                  style={{ width: `${fillPercent}%` }}
                />
              </div>
              <div className="flex w-full items-center justify-end">
                <p className={SHELL_AI_CREDITS_LEFT_LINE_CLASS}>
                  {credits.leftLine}
                </p>
              </div>
            </div>
          </div>
        </div>
        {credits.showViewUsage || credits.showAddCredits ? (
          <div className={SHELL_AI_CREDITS_FOOTER_CLASS}>
            {credits.showViewUsage ? (
              <Button
                type="button"
                variant="op-tertiary"
                size="op"
                className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                onClick={() => {
                  handleOpenChange(false)
                  onViewUsage()
                }}
              >
                {credits.viewUsageLabel}
              </Button>
            ) : null}
            {credits.showAddCredits ? (
              <Button
                type="button"
                variant="op-secondary"
                size="op"
                className={cn(GUESTS_PAGE_SECONDARY_BUTTON_CLASS)}
                onClick={() => {
                  handleOpenChange(false)
                  onAddCredits()
                }}
              >
                {credits.addCreditsLabel}
              </Button>
            ) : null}
          </div>
        ) : null}
      </PopoverContent>
    </Popover>
  )
}
