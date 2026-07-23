import type { ReactNode } from "react"

import { GuestProfileEmptyCopy } from "@/components/dashboard/operator/GuestProfile/GuestProfileEmptyCopy"
import {
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestProfileSectionEmptyCardProps = {
  sectionTitle: string
  headerAction?: ReactNode
  emptyTitle: string
  emptyHelper: string
  emptyFooter?: ReactNode
}

export function GuestProfileSectionEmptyCard({
  sectionTitle,
  headerAction,
  emptyTitle,
  emptyHelper,
  emptyFooter,
}: GuestProfileSectionEmptyCardProps) {
  return (
    <section className={GUESTS_SECTION_CLASS}>
      <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{sectionTitle}</h2>
        {headerAction ?? null}
      </div>
      <GuestProfileEmptyCopy
        title={emptyTitle}
        helper={emptyHelper}
        footer={emptyFooter}
      />
    </section>
  )
}
