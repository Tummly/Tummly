import { Button } from "@/components/ui/button"
import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import {
  ADDITIONAL_GROUP_LOCATION_COPY,
  type AdditionalGroupLocationViewModel,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  GUESTS_DETAIL_FIELD_CLASS,
  GUESTS_DETAIL_FIELD_LABEL_CLASS,
  GUESTS_DETAIL_FIELD_VALUE_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

function CountRow({ label, value }: { label: string; value: string }) {
  return (
    <div className={GUESTS_DETAIL_FIELD_CLASS}>
      <p className={GUESTS_DETAIL_FIELD_LABEL_CLASS}>{label}</p>
      <p className={GUESTS_DETAIL_FIELD_VALUE_CLASS}>{value}</p>
    </div>
  )
}

export function ManagePlanAdditionalGroupLocationSection({
  viewModel,
  showActions,
  actionsEnabled = true,
  pageModule,
  extraLocationConfirm,
}: {
  viewModel: AdditionalGroupLocationViewModel
  showActions: boolean
  actionsEnabled?: boolean
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
  extraLocationConfirm: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >["extraLocationConfirm"]
}) {
  return (
    <>
      <section className={GUESTS_SECTION_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>
          {ADDITIONAL_GROUP_LOCATION_COPY.sectionTitle}
        </h2>
        <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
          <CountRow
            label={ADDITIONAL_GROUP_LOCATION_COPY.included}
            value={String(viewModel.includedCount)}
          />
          <CountRow
            label={ADDITIONAL_GROUP_LOCATION_COPY.extra}
            value={String(viewModel.extraCount)}
          />
          <CountRow
            label={ADDITIONAL_GROUP_LOCATION_COPY.total}
            value={String(viewModel.totalCount)}
          />
          <CountRow
            label={ADDITIONAL_GROUP_LOCATION_COPY.cap}
            value={String(viewModel.cap)}
          />
        </div>

        {showActions ? (
          <div className="flex flex-wrap gap-3 pt-4">
            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              disabled={!actionsEnabled || !viewModel.canAdd}
              onClick={() => {
                pageModule.requestAddExtraLocation()
              }}
            >
              {ADDITIONAL_GROUP_LOCATION_COPY.addLocation}
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              disabled={!actionsEnabled || !viewModel.canRemove}
              onClick={() => {
                pageModule.requestRemoveExtraLocation()
              }}
            >
              {ADDITIONAL_GROUP_LOCATION_COPY.removeLocation}
            </Button>
          </div>
        ) : null}
      </section>

      {extraLocationConfirm?.open ? (
        <AccountWorkspaceConfirmDialog
          open
          title={extraLocationConfirm.title}
          body={extraLocationConfirm.body}
          primaryLabel={extraLocationConfirm.primaryLabel}
          busy={extraLocationConfirm.busy}
          onOpenChange={(open) => {
            if (!open) {
              pageModule.cancelExtraLocationChange()
            }
          }}
          onPrimary={() => {
            void pageModule.confirmExtraLocationChange()
          }}
          onCancel={() => {
            pageModule.cancelExtraLocationChange()
          }}
        />
      ) : null}
    </>
  )
}

export function ManagePlanCancelPlanControl({
  showCancelPlan,
  cancelEnabled = true,
  pageModule,
  cancelPlanConfirm,
}: {
  showCancelPlan: boolean
  cancelEnabled?: boolean
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
  cancelPlanConfirm: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >["cancelPlanConfirm"]
}) {
  if (!showCancelPlan) {
    return null
  }

  return (
    <>
      <div className="flex justify-center pt-2">
        <Button
          type="button"
          variant="op-link"
          disabled={!cancelEnabled}
          onClick={() => {
            pageModule.requestCancelPlan()
          }}
        >
          {ADDITIONAL_GROUP_LOCATION_COPY.cancelPlan}
        </Button>
      </div>

      {cancelPlanConfirm?.open ? (
        <AccountWorkspaceConfirmDialog
          open
          title={cancelPlanConfirm.title}
          body={cancelPlanConfirm.body}
          primaryLabel={cancelPlanConfirm.primaryLabel}
          busy={cancelPlanConfirm.busy}
          onOpenChange={(open) => {
            if (!open) {
              pageModule.cancelCancelPlan()
            }
          }}
          onPrimary={() => {
            void pageModule.confirmCancelPlan()
          }}
          onCancel={() => {
            pageModule.cancelCancelPlan()
          }}
        />
      ) : null}
    </>
  )
}
