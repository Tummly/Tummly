import { Fragment } from "react"

import {
  GUESTS_DETAIL_DIVIDER_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  LOCATIONS_ACTIVITY_ITEM_BODY_CLASS,
  LOCATIONS_ACTIVITY_ITEM_TIME_CLASS,
  LOCATIONS_CARD_CLASS,
  LOCATIONS_PAGE_COPY,
  type LocationsActivityItem,
} from "@/lib/operatorLocations/locationsPresentation"
import { cn } from "@/lib/utils"

type LocationsActivitySectionProps = {
  items: readonly LocationsActivityItem[]
}

/** Locations Activity card — Figma 5748:104239. */
export function LocationsActivitySection({
  items,
}: LocationsActivitySectionProps) {
  const copy = LOCATIONS_PAGE_COPY

  return (
    <section
      className={cn(LOCATIONS_CARD_CLASS, "gap-10")}
      aria-label={copy.activityTitle}
    >
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.activityTitle}</h2>
      </header>

      {items.length === 0 ? (
        <div className="flex flex-col gap-2">
          <p className="m-0 text-sm font-medium text-op-text-primary">
            {copy.activityEmptyTitle}
          </p>
          <p className="m-0 text-sm font-medium text-op-card-subtitle-color">
            {copy.activityEmptyBody}
          </p>
        </div>
      ) : (
        <div className="flex w-full flex-col gap-[22px]">
          {items.map((item, index) => (
            <Fragment key={item.id}>
              {index > 0 ? (
                <hr className={GUESTS_DETAIL_DIVIDER_CLASS} />
              ) : null}
              <div className="flex flex-col gap-2">
                <p className={LOCATIONS_ACTIVITY_ITEM_TIME_CLASS}>
                  {item.timeLabel}
                </p>
                <p className={LOCATIONS_ACTIVITY_ITEM_BODY_CLASS}>
                  {item.description}
                </p>
              </div>
            </Fragment>
          ))}
        </div>
      )}
    </section>
  )
}
