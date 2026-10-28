import { Fragment } from "react"

import { Badge } from "@/components/ui/badge"
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
  type PrivacyArchivedActivityItem,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"
import { cn } from "@/lib/utils"

type PrivacyArchivedActivitySectionProps = {
  items: readonly PrivacyArchivedActivityItem[]
  /** Set when the API refuses archived history to this member. */
  accessRestricted?: boolean
}

/**
 * Owner-only, read-only history for archived Locations. Rendered apart from
 * the active Privacy activity list and never used for marketing eligibility.
 */
export function PrivacyArchivedActivitySection({
  items,
  accessRestricted = false,
}: PrivacyArchivedActivitySectionProps) {
  const copy = PRIVACY_CONSENT_PAGE_COPY

  return (
    <section
      className={cn(PRIVACY_CONSENT_CARD_CLASS, "gap-10")}
      aria-label={copy.archivedLocationsTitle}
    >
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>
          {copy.archivedLocationsTitle}
        </h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
          {copy.archivedLocationsSubtitle}
        </p>
      </header>

      {accessRestricted ? (
        <div className="flex flex-col gap-2">
          <p className="m-0 text-sm font-medium text-op-text-primary">
            {copy.archivedLocationsAccessRestrictedTitle}
          </p>
          <p className="m-0 text-sm font-medium text-op-card-subtitle-color">
            {copy.archivedLocationsAccessRestrictedBody}
          </p>
        </div>
      ) : items.length === 0 ? (
        <div className="flex flex-col gap-2">
          <p className="m-0 text-sm font-medium text-op-text-primary">
            {copy.archivedLocationsEmptyTitle}
          </p>
          <p className="m-0 text-sm font-medium text-op-card-subtitle-color">
            {copy.archivedLocationsEmptyBody}
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
                <div className="flex flex-wrap items-center gap-2">
                  <Badge variant="outline">{copy.archivedLocationBadge}</Badge>
                  <span className="text-sm font-medium text-op-text-primary">
                    {item.locationName}
                  </span>
                </div>
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
