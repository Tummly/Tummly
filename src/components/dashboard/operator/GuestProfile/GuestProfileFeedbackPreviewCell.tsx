import { useLayoutEffect, useRef, useState } from "react"

import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import { GUESTS_TABLE_GUEST_NAME_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import {
  OPERATOR_SHELL_TOOLTIP_ARROW_CLASS,
  OPERATOR_SHELL_TOOLTIP_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"

type GuestProfileFeedbackPreviewCellProps = {
  /** Visible cell text (CSS-truncated). */
  text: string
  /** Tooltip body when truncated; defaults to `text`. */
  tooltipText?: string
}

/**
 * Feedback table cell that truncates with ellipsis and only opens a tooltip
 * when the visible text actually overflows its container.
 */
export function GuestProfileFeedbackPreviewCell({
  text,
  tooltipText = text,
}: GuestProfileFeedbackPreviewCellProps) {
  const textRef = useRef<HTMLSpanElement>(null)
  const [isOverflowing, setIsOverflowing] = useState(false)
  const [open, setOpen] = useState(false)

  useLayoutEffect(() => {
    const el = textRef.current
    if (el == null) {
      return
    }

    const update = () => {
      const overflowing = el.scrollWidth > el.clientWidth + 1
      setIsOverflowing(overflowing)
      if (!overflowing) {
        setOpen(false)
      }
    }

    update()

    const observer = new ResizeObserver(update)
    observer.observe(el)
    return () => {
      observer.disconnect()
    }
  }, [text])

  return (
    <Tooltip
      open={open}
      onOpenChange={(nextOpen) => {
        if (nextOpen && !isOverflowing) {
          return
        }
        setOpen(nextOpen)
      }}
    >
      <TooltipTrigger asChild>
        <span
          ref={textRef}
          className={`${GUESTS_TABLE_GUEST_NAME_CLASS} block max-w-56 min-w-0 truncate`}
        >
          {text}
        </span>
      </TooltipTrigger>
      {isOverflowing ? (
        <TooltipContent
          side="top"
          sideOffset={6}
          className={`${OPERATOR_SHELL_TOOLTIP_CONTENT_CLASS} max-w-sm px-3 py-2 text-left text-xs leading-5 whitespace-normal`}
          arrowClassName={OPERATOR_SHELL_TOOLTIP_ARROW_CLASS}
        >
          {tooltipText}
        </TooltipContent>
      ) : null}
    </Tooltip>
  )
}
