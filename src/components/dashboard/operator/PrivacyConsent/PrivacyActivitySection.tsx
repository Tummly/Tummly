import { Fragment } from "react"

import {
  GUESTS_DETAIL_DIVIDER_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  LOCATIONS_ACTIVITY_ITEM_BODY_CLASS,
  LOCATIONS_ACTIVITY_ITEM_TIME_CLASS,
} from "@/lib/operatorLocations/locationsPresentation"
import {
  PRIVACY_CONSENT_CARD_CLASS,
  PRIVACY_CONSENT_PAGE_COPY,
  type PrivacyActivityItem,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"
import { cn } from "@/lib/utils"

type PrivacyActivitySectionProps = {
  items: readonly PrivacyActivityItem[]
}

/** Privacy activity card — Figma 5746:101810. */
export function PrivacyActivitySection({
  items,
}: PrivacyActivitySectionProps) {
  const copy = PRIVACY_CONSENT_PAGE_COPY

  return (
    <section
      className={cn(PRIVACY_CONSENT_CARD_CLASS, "gap-10")}
      aria-label={copy.privacyActivityTitle}
    >
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>
          {copy.privacyActivityTitle}
        </h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
          {copy.privacyActivitySubtitle}
        </p>
      </header>

      {items.length === 0 ? (
        <div className="flex flex-col gap-2">
          <p className="m-0 text-sm font-medium text-op-text-primary">
            {copy.privacyActivityEmptyTitle}
          </p>
          <p className="m-0 text-sm font-medium text-op-card-subtitle-color">
            {copy.privacyActivityEmptyBody}
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
