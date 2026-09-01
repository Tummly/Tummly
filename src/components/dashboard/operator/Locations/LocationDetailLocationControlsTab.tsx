import { Button } from "@/components/ui/button"
import type { LocationDetailSnapshot } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
import { useLocationDetailPageModuleApi } from "@/components/dashboard/operator/Locations/utils/locationDetailPageModuleContext"
import {
  LOCATION_CONTROLS_STATUS_LABELS,
  LOCATION_CONTROLS_STATUS_ROWS,
  LOCATION_DETAIL_ACTION_BUTTON_CLASS,
  LOCATION_DETAIL_CARD_CLASS,
  LOCATION_DETAIL_METRIC_DIVIDER_CLASS,
  LOCATION_DETAIL_METRIC_FIELD_CLASS,
  LOCATION_DETAIL_METRIC_LABEL_CLASS,
  LOCATION_DETAIL_METRIC_PAIR_CLASS,
  LOCATION_DETAIL_METRIC_STACK_CLASS,
  LOCATION_DETAIL_METRIC_VALUE_CLASS,
  LOCATION_DETAIL_PAGE_COPY,
  LOCATION_DETAIL_SECTION_SUBTITLE_CLASS,
  LOCATION_DETAIL_SECTION_TITLE_CLASS,
  type LocationControlsLifecycleActionId,
} from "@/lib/operatorLocations/locationDetailPresentation"
import { cn } from "@/lib/utils"

type LocationDetailLocationControlsTabProps = {
  snap: LocationDetailSnapshot
}

export function LocationDetailLocationControlsTab({
  snap,
}: LocationDetailLocationControlsTabProps) {
  const copy = LOCATION_DETAIL_PAGE_COPY
  const pageModule = useLocationDetailPageModuleApi()

  const onLifecycleAction = (actionId: LocationControlsLifecycleActionId) => {
    pageModule.requestLifecycleAction(actionId)
  }

  return (
    <div className="flex flex-col gap-5">
      <section
        className={cn(LOCATION_DETAIL_CARD_CLASS, "gap-7")}
        aria-label={copy.locationStatusTitle}
      >
        <h2 className={LOCATION_DETAIL_SECTION_TITLE_CLASS}>
          {copy.locationStatusTitle}
        </h2>

        <div className={LOCATION_DETAIL_METRIC_STACK_CLASS}>
          {LOCATION_CONTROLS_STATUS_ROWS.map(([leftId, rightId], index) => (
            <div key={leftId} className="flex flex-col gap-5">
              <div className={LOCATION_DETAIL_METRIC_PAIR_CLASS}>
                <div className={LOCATION_DETAIL_METRIC_FIELD_CLASS}>
                  <p className={LOCATION_DETAIL_METRIC_LABEL_CLASS}>
                    {LOCATION_CONTROLS_STATUS_LABELS[leftId]}
                  </p>
                  <p className={LOCATION_DETAIL_METRIC_VALUE_CLASS}>
                    {snap.locationControlsStatus[leftId]}
                  </p>
                </div>
                {rightId != null ? (
                  <div className={LOCATION_DETAIL_METRIC_FIELD_CLASS}>
                    <p className={LOCATION_DETAIL_METRIC_LABEL_CLASS}>
                      {LOCATION_CONTROLS_STATUS_LABELS[rightId]}
                    </p>
                    <p className={LOCATION_DETAIL_METRIC_VALUE_CLASS}>
                      {snap.locationControlsStatus[rightId]}
                    </p>
                  </div>
                ) : (
                  <div className="hidden sm:block" aria-hidden />
                )}
              </div>
              {index < LOCATION_CONTROLS_STATUS_ROWS.length - 1 ? (
                <hr className={LOCATION_DETAIL_METRIC_DIVIDER_CLASS} />
              ) : null}
            </div>
          ))}
        </div>
      </section>

      <section
        className={cn(LOCATION_DETAIL_CARD_CLASS, "gap-10")}
        aria-label={copy.dangerZoneTitle}
      >
        <div className="flex flex-col gap-2">
          <h2 className={LOCATION_DETAIL_SECTION_TITLE_CLASS}>
            {copy.dangerZoneTitle}
          </h2>
          <p className={LOCATION_DETAIL_SECTION_SUBTITLE_CLASS}>
            {copy.dangerZoneSubtitle}
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          {snap.locationControlsActions.map((action) => (
            <Button
              key={action.id}
              type="button"
              variant={action.variant}
              className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
              disabled={!action.enabled || snap.lifecycleMutationPending}
              onClick={() => {
                onLifecycleAction(action.id)
              }}
            >
              {action.label}
            </Button>
          ))}
        </div>
      </section>
    </div>
  )
}
