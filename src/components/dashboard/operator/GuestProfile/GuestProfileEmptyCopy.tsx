import type { ReactNode } from "react"

import {
  GUESTS_TABLE_EMPTY_ACTIONS_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestProfileEmptyCopyProps = {
  title: string
  helper: string
  footer?: ReactNode
}

export function GuestProfileEmptyCopy({
  title,
  helper,
  footer,
}: GuestProfileEmptyCopyProps) {
  return (
    <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
      <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
        <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{title}</p>
        <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>{helper}</p>
      </div>
      {footer ? (
        <div className={GUESTS_TABLE_EMPTY_ACTIONS_CLASS}>{footer}</div>
      ) : null}
    </div>
  )
}
