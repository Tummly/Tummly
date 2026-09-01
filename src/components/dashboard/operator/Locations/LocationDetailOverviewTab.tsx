import type { LocationDetailSnapshot } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
import { LocationDetailAssignedQrSection } from "@/components/dashboard/operator/Locations/LocationDetailAssignedQrSection"
import { LocationDetailOffersSection } from "@/components/dashboard/operator/Locations/LocationDetailOffersSection"
import {
  formatLocationDetailMonthMetric,
  LOCATION_DETAIL_CARD_CLASS,
  LOCATION_DETAIL_METRIC_DIVIDER_CLASS,
  LOCATION_DETAIL_METRIC_FIELD_CLASS,
  LOCATION_DETAIL_METRIC_LABEL_CLASS,
  LOCATION_DETAIL_METRIC_PAIR_CLASS,
  LOCATION_DETAIL_METRIC_STACK_CLASS,
  LOCATION_DETAIL_METRIC_VALUE_CLASS,
  LOCATION_DETAIL_OVERVIEW_METRIC_LABELS,
  LOCATION_DETAIL_OVERVIEW_METRIC_ROWS,
  LOCATION_DETAIL_PAGE_COPY,
  LOCATION_DETAIL_SECTION_TITLE_CLASS,
} from "@/lib/operatorLocations/locationDetailPresentation"
import { cn } from "@/lib/utils"

type LocationDetailOverviewTabProps = {
  snap: LocationDetailSnapshot
  createQrPath: string
  createOfferPath: string
  createCampaignPath: string
}

export function LocationDetailOverviewTab({
  snap,
  createQrPath,
  createOfferPath,
  createCampaignPath,
}: LocationDetailOverviewTabProps) {
  const copy = LOCATION_DETAIL_PAGE_COPY

  return (
    <div className="flex flex-col gap-5">
      <section className={cn(LOCATION_DETAIL_CARD_CLASS, "gap-7")} aria-label="Overview">
        <h2 className={LOCATION_DETAIL_SECTION_TITLE_CLASS}>{copy.overviewTitle}</h2>
        <div className={LOCATION_DETAIL_METRIC_STACK_CLASS}>
          {LOCATION_DETAIL_OVERVIEW_METRIC_ROWS.map(([leftId, rightId], index) => (
            <div key={leftId} className="flex flex-col gap-5">
              <div className={LOCATION_DETAIL_METRIC_PAIR_CLASS}>
                <div className={LOCATION_DETAIL_METRIC_FIELD_CLASS}>
                  <p className={LOCATION_DETAIL_METRIC_LABEL_CLASS}>
                    {LOCATION_DETAIL_OVERVIEW_METRIC_LABELS[leftId]}
                  </p>
                  <p className={LOCATION_DETAIL_METRIC_VALUE_CLASS}>
                    {formatLocationDetailMonthMetric(
                      snap.overviewMetrics[leftId]
                    )}
                  </p>
                </div>
                {rightId != null ? (
                  <div className={LOCATION_DETAIL_METRIC_FIELD_CLASS}>
                    <p className={LOCATION_DETAIL_METRIC_LABEL_CLASS}>
                      {LOCATION_DETAIL_OVERVIEW_METRIC_LABELS[rightId]}
                    </p>
                    <p className={LOCATION_DETAIL_METRIC_VALUE_CLASS}>
                      {formatLocationDetailMonthMetric(
                        snap.overviewMetrics[rightId]
                      )}
                    </p>
                  </div>
                ) : (
                  <div className="hidden sm:block" aria-hidden />
                )}
              </div>
              {index < LOCATION_DETAIL_OVERVIEW_METRIC_ROWS.length - 1 ? (
                <hr className={LOCATION_DETAIL_METRIC_DIVIDER_CLASS} />
              ) : null}
            </div>
          ))}
        </div>
      </section>

      <LocationDetailAssignedQrSection
        qrRows={snap.qrRows}
        createQrPath={createQrPath}
      />

      <LocationDetailOffersSection
        offerCards={snap.offerCards}
        createOfferPath={createOfferPath}
        createCampaignPath={createCampaignPath}
      />
    </div>
  )
}
