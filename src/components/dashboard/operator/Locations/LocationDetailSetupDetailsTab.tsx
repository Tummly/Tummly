import type { LocationDetailSnapshot } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
import {
  formatLocationSetupChecklistStatus,
  LOCATION_DETAIL_CARD_CLASS,
  LOCATION_DETAIL_METRIC_DIVIDER_CLASS,
  LOCATION_DETAIL_METRIC_FIELD_CLASS,
  LOCATION_DETAIL_METRIC_LABEL_CLASS,
  LOCATION_DETAIL_METRIC_PAIR_CLASS,
  LOCATION_DETAIL_METRIC_STACK_CLASS,
  LOCATION_DETAIL_METRIC_VALUE_CLASS,
  LOCATION_DETAIL_PAGE_COPY,
  LOCATION_DETAIL_SECTION_TITLE_CLASS,
  LOCATION_SETUP_CHECKLIST_LABELS,
  LOCATION_SETUP_CHECKLIST_ROWS,
} from "@/lib/operatorLocations/locationDetailPresentation"
import { cn } from "@/lib/utils"

type LocationDetailSetupDetailsTabProps = {
  snap: LocationDetailSnapshot
}

export function LocationDetailSetupDetailsTab({
  snap,
}: LocationDetailSetupDetailsTabProps) {
  const copy = LOCATION_DETAIL_PAGE_COPY

  return (
    <section
      className={cn(LOCATION_DETAIL_CARD_CLASS, "gap-7")}
      aria-label={copy.locationSetupTitle}
    >
      <h2 className={LOCATION_DETAIL_SECTION_TITLE_CLASS}>
        {copy.locationSetupTitle}
      </h2>

      <div className={LOCATION_DETAIL_METRIC_STACK_CLASS}>
        {LOCATION_SETUP_CHECKLIST_ROWS.map(([leftId, rightId], index) => (
          <div key={leftId} className="flex flex-col gap-5">
            <div className={LOCATION_DETAIL_METRIC_PAIR_CLASS}>
              <div className={LOCATION_DETAIL_METRIC_FIELD_CLASS}>
                <p className={LOCATION_DETAIL_METRIC_LABEL_CLASS}>
                  {LOCATION_SETUP_CHECKLIST_LABELS[leftId]}
                </p>
                <p className={LOCATION_DETAIL_METRIC_VALUE_CLASS}>
                  {formatLocationSetupChecklistStatus(
                    snap.setupChecklist[leftId]
                  )}
                </p>
              </div>
              {rightId != null ? (
                <div className={LOCATION_DETAIL_METRIC_FIELD_CLASS}>
                  <p className={LOCATION_DETAIL_METRIC_LABEL_CLASS}>
                    {LOCATION_SETUP_CHECKLIST_LABELS[rightId]}
                  </p>
                  <p className={LOCATION_DETAIL_METRIC_VALUE_CLASS}>
                    {formatLocationSetupChecklistStatus(
                      snap.setupChecklist[rightId]
                    )}
                  </p>
                </div>
              ) : (
                <div className="hidden sm:block" aria-hidden />
              )}
            </div>
            {index < LOCATION_SETUP_CHECKLIST_ROWS.length - 1 ? (
              <hr className={LOCATION_DETAIL_METRIC_DIVIDER_CLASS} />
            ) : null}
          </div>
        ))}
      </div>
    </section>
  )
}
