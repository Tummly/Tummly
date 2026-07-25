import { SearchIcon } from "lucide-react"
import type { ReactNode } from "react"

import { GuestProfileEmptyCopy } from "@/components/dashboard/operator/GuestProfile/GuestProfileEmptyCopy"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { GUEST_PROFILE_DEFAULT_SORT_LABEL } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_SORT_BUTTON_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestProfileTableEmptyCardProps = {
  sectionTitle: string
  headerAction?: ReactNode
  searchPlaceholder: string
  sortLabel?: string
  emptyTitle: string
  emptyHelper: string
}

export function GuestProfileTableEmptyCard({
  sectionTitle,
  headerAction,
  searchPlaceholder,
  sortLabel = GUEST_PROFILE_DEFAULT_SORT_LABEL,
  emptyTitle,
  emptyHelper,
}: GuestProfileTableEmptyCardProps) {
  return (
    <section className={GUESTS_SECTION_CLASS}>
      <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{sectionTitle}</h2>
        {headerAction ?? null}
      </div>

      <div className={GUESTS_TOOLBAR_ROW_CLASS}>
        <div className={GUESTS_SEARCH_WRAP_CLASS}>
          <SearchIcon
            className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-[#707070]"
            aria-hidden
          />
          <Input
            disabled
            aria-disabled
            aria-label={`${searchPlaceholder} (unavailable)`}
            title={`${searchPlaceholder} is unavailable`}
            placeholder={searchPlaceholder}
            className={GUESTS_SEARCH_FIELD_CLASS}
          />
        </div>

        <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-secondary"
            disabled
            aria-disabled
            aria-label="Filters (unavailable)"
            title="Filters is unavailable"
            className="rounded-[2px]"
          >
            Filters
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled
            aria-disabled
            aria-label={`${sortLabel} (unavailable)`}
            title={`${sortLabel} is unavailable`}
            className={GUESTS_SORT_BUTTON_CLASS}
          >
            {sortLabel}
          </Button>
        </div>
      </div>

      <GuestProfileEmptyCopy title={emptyTitle} helper={emptyHelper} />
    </section>
  )
}
