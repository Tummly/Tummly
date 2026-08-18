import type { LucideIcon } from "lucide-react"
import {
  BanknoteIcon,
  RefreshCwIcon,
  SquarePenIcon,
  TagIcon,
  TicketPercentIcon,
} from "lucide-react"
import { useLocation } from "react-router-dom"

import { CreateEditOfferDrawer } from "@/components/dashboard/operator/Offers/CreateEditOfferDrawer"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import type { CampaignExistingOfferPickerCard } from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"
import { CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS } from "@/lib/operatorCampaigns/campaignTemplatePickerPresentation"
import type {
  RecoveryExistingOfferPickerViewModel,
  RespondWithRecoveryOfferSnapshot,
} from "@/lib/operatorFeedback/createRespondWithRecoveryOfferModule"
import {
  RECOVERY_EXISTING_OFFER_PICKER_CARD_ACTIONS_CLASS,
  RECOVERY_EXISTING_OFFER_PICKER_CARD_CLASS,
  RECOVERY_EXISTING_OFFER_PICKER_CARD_META_CLASS,
  RECOVERY_EXISTING_OFFER_PICKER_CARD_TITLE_CLASS,
  RECOVERY_EXISTING_OFFER_PICKER_ICON_WELL_CLASS,
  RECOVERY_EXISTING_OFFER_PICKER_PANEL_CLASS,
  RECOVERY_OFFER_STEP_COPY,
  type RecoveryOfferStanceId,
  type RecoveryOfferStanceOptionViewModel,
} from "@/lib/operatorFeedback/recoveryOfferPresentation"
import type { CampaignCatalogOfferDetailsDraft } from "@/lib/operatorOffers/offerCatalogPresentation"
import {
  operatorDashboardOfferDetailsPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import {
  OPERATOR_WIZARD_SELECTABLE_CARD_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_DISABLED_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_IDLE_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_SELECTED_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS,
} from "@/lib/operatorUi/operatorWizardChromePresentation"
import { cn } from "@/lib/utils"

const OFFER_STANCE_ICONS: Record<RecoveryOfferStanceId, LucideIcon> = {
  "create-and-select": SquarePenIcon,
  "existing-offer": TagIcon,
}

const PICKER_TYPE_ICONS: Record<
  CampaignExistingOfferPickerCard["offerTypeIconId"],
  LucideIcon
> = {
  percentage_discount: TicketPercentIcon,
  fixed_discount: BanknoteIcon,
  free_item: TagIcon,
  replacement_item: RefreshCwIcon,
  unknown: TagIcon,
}

type RecoveryOfferStepProps = {
  snapshot: RespondWithRecoveryOfferSnapshot
  disabled?: boolean
  onSelectStance: (stanceId: RecoveryOfferStanceId) => void
  onCloseCreatePanel: () => void
  onEditAttachedOffer: () => void
  onPatchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  onConfirmCreateOffer: () => void
  onExistingOfferSearchChange: (query: string) => void
  onSelectExistingOffer: (offerId: number) => void
  onRetryExistingOfferPicker: () => void
}

function useOperatorDashboardMode(): OperatorDashboardMode {
  const { pathname } = useLocation()
  return pathname.startsWith("/multi-dashboard") ? "multi" : "single"
}

function OfferStanceCard({
  option,
  onSelect,
}: {
  option: RecoveryOfferStanceOptionViewModel
  onSelect: () => void
}) {
  const Icon = OFFER_STANCE_ICONS[option.id]

  return (
    <Button
      type="button"
      variant="ghost"
      role="radio"
      aria-checked={option.selected}
      aria-disabled={option.disabled || undefined}
      disabled={option.disabled}
      className={cn(
        OPERATOR_WIZARD_SELECTABLE_CARD_CLASS,
        "min-h-0 gap-2.5",
        option.disabled
          ? OPERATOR_WIZARD_SELECTABLE_CARD_DISABLED_CLASS
          : option.selected
            ? OPERATOR_WIZARD_SELECTABLE_CARD_SELECTED_CLASS
            : OPERATOR_WIZARD_SELECTABLE_CARD_IDLE_CLASS
      )}
      onClick={onSelect}
    >
      <span className="flex size-9 shrink-0 items-center justify-center rounded-[2px] bg-op-background-secondary p-2.5">
        <Icon className="size-4 text-op-text-primary" aria-hidden />
      </span>
      <span className="flex min-w-0 flex-col gap-1">
        <span className="text-sm font-medium leading-normal text-op-text-primary">
          {option.title}
        </span>
        <span className="text-xs font-medium leading-normal text-[var(--op-color-gray-550)]">
          {option.description}
        </span>
      </span>
    </Button>
  )
}

function AttachedOfferSummary({
  title,
  onEdit,
  disabled,
}: {
  title: string
  onEdit: () => void
  disabled?: boolean
}) {
  return (
    <div
      className={cn(
        "flex w-full items-center justify-between gap-3 rounded-[4px] border p-4 sm:px-4 sm:py-3",
        OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS
      )}
    >
      <div className="flex min-w-0 flex-col gap-1">
        <p className="m-0 text-xs font-medium text-[var(--op-color-gray-550)]">
          Attached offer
        </p>
        <p className="m-0 text-sm font-medium text-op-text-primary">{title}</p>
      </div>
      <Button
        type="button"
        variant="op-secondary"
        className="h-8 shrink-0 px-3"
        disabled={disabled}
        onClick={onEdit}
      >
        {RECOVERY_OFFER_STEP_COPY.attachedSummaryEdit}
      </Button>
    </div>
  )
}

function ExistingOfferPickerCardRow({
  card,
  selectLabel,
  viewDetailsLabel,
  viewDetailsHref,
  onSelect,
  disabled,
}: {
  card: CampaignExistingOfferPickerCard
  selectLabel: string
  viewDetailsLabel: string
  viewDetailsHref: string | null
  onSelect: () => void
  disabled?: boolean
}) {
  const Icon = PICKER_TYPE_ICONS[card.offerTypeIconId]

  return (
    <article className={RECOVERY_EXISTING_OFFER_PICKER_CARD_CLASS}>
      <div className="flex items-center gap-4">
        <span className={RECOVERY_EXISTING_OFFER_PICKER_ICON_WELL_CLASS}>
          <Icon className="size-5 text-op-text-primary" aria-hidden />
        </span>
        <div className="flex min-w-0 flex-col gap-2">
          <p className={RECOVERY_EXISTING_OFFER_PICKER_CARD_TITLE_CLASS}>
            {card.title}
          </p>
          <p className={RECOVERY_EXISTING_OFFER_PICKER_CARD_META_CLASS}>
            <span>{card.validUntilLabel}</span>
            <span aria-hidden>·</span>
            <span>{card.useRuleLabel}</span>
          </p>
        </div>
      </div>
      <div className={RECOVERY_EXISTING_OFFER_PICKER_CARD_ACTIONS_CLASS}>
        <Button
          type="button"
          variant="op-secondary"
          disabled={disabled}
          onClick={onSelect}
        >
          {selectLabel}
        </Button>
        {viewDetailsHref != null ? (
          <Button asChild variant="op-tertiary">
            <a
              href={viewDetailsHref}
              target="_blank"
              rel="noopener noreferrer"
            >
              {viewDetailsLabel}
            </a>
          </Button>
        ) : null}
      </div>
    </article>
  )
}

function ExistingOfferPicker({
  picker,
  dashboardMode,
  locationId,
  disabled,
  onSearchChange,
  onSelect,
  onRetry,
}: {
  picker: RecoveryExistingOfferPickerViewModel
  dashboardMode: OperatorDashboardMode
  locationId: number | null
  disabled?: boolean
  onSearchChange: (query: string) => void
  onSelect: (offerId: number) => void
  onRetry: () => void
}) {
  return (
    <div
      className={RECOVERY_EXISTING_OFFER_PICKER_PANEL_CLASS}
      data-testid="recovery-existing-offer-picker"
    >
      <div className="relative w-full">
        <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-header-search-text" />
        <Input
          value={picker.searchQuery}
          onChange={(event) => {
            onSearchChange(event.target.value)
          }}
          placeholder={picker.searchPlaceholder}
          className={CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS}
          aria-label={picker.searchPlaceholder}
          disabled={disabled}
        />
      </div>

      {picker.loadStatus === "loading" ? (
        <p className="m-0 text-sm font-medium text-[var(--op-color-gray-550)]">
          Loading offers…
        </p>
      ) : null}

      {picker.loadStatus === "error" ? (
        <div className="flex flex-col gap-3">
          <p className="m-0 text-sm font-medium leading-5 text-op-text-muted">
            {picker.error}
          </p>
          <Button
            type="button"
            variant="op-tertiary"
            className="w-fit"
            disabled={disabled}
            onClick={onRetry}
          >
            {picker.retryLabel}
          </Button>
        </div>
      ) : null}

      {picker.loadStatus === "ready" && picker.isEmpty ? (
        <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
          {picker.emptyHelper}
        </p>
      ) : null}

      {picker.loadStatus === "ready" && !picker.isEmpty
        ? picker.cards.map((card) => (
            <ExistingOfferPickerCardRow
              key={card.id}
              card={card}
              selectLabel={picker.selectLabel}
              viewDetailsLabel={picker.viewDetailsLabel}
              viewDetailsHref={
                picker.viewDetailsEnabled && locationId != null
                  ? operatorDashboardOfferDetailsPath(
                      dashboardMode,
                      card.id,
                      locationId
                    )
                  : null
              }
              disabled={disabled}
              onSelect={() => {
                onSelect(card.id)
              }}
            />
          ))
        : null}
    </div>
  )
}

/**
 * Recovery Offer step — Create and select (ticket 04) + Existing attach (ticket 03).
 */
export function RecoveryOfferStep({
  snapshot,
  disabled = false,
  onSelectStance,
  onCloseCreatePanel,
  onEditAttachedOffer,
  onPatchCreateOfferDraft,
  onConfirmCreateOffer,
  onExistingOfferSearchChange,
  onSelectExistingOffer,
  onRetryExistingOfferPicker,
}: RecoveryOfferStepProps) {
  const dashboardMode = useOperatorDashboardMode()
  const picker = snapshot.existingOfferPicker
  const showAttachedSummary = snapshot.offerId != null && picker == null

  return (
    <>
      <div className="flex w-full max-w-[690px] flex-col gap-7">
        <header className="flex flex-col gap-2">
          <h2 className="m-0 text-xl font-semibold leading-normal text-op-text-primary sm:text-[22px]">
            {RECOVERY_OFFER_STEP_COPY.stepHeading}
          </h2>
          <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {RECOVERY_OFFER_STEP_COPY.stepDescription}
          </p>
        </header>

        <div
          className="flex w-full flex-col gap-[18px]"
          role="radiogroup"
          aria-label={RECOVERY_OFFER_STEP_COPY.stepHeading}
        >
          {snapshot.offerStanceOptions.map((option) => (
            <OfferStanceCard
              key={option.id}
              option={option}
              onSelect={() => {
                if (!option.disabled && !disabled) {
                  onSelectStance(option.id)
                }
              }}
            />
          ))}
        </div>

        {picker != null ? (
          <ExistingOfferPicker
            picker={picker}
            dashboardMode={dashboardMode}
            locationId={snapshot.locationId}
            disabled={disabled}
            onSearchChange={onExistingOfferSearchChange}
            onSelect={onSelectExistingOffer}
            onRetry={onRetryExistingOfferPicker}
          />
        ) : null}

        {showAttachedSummary ? (
          <AttachedOfferSummary
            title={
              snapshot.attachedOfferTitle
              ?? RECOVERY_OFFER_STEP_COPY.attachedSummaryFallbackTitle
            }
            disabled={disabled}
            onEdit={onEditAttachedOffer}
          />
        ) : null}
      </div>

      <CreateEditOfferDrawer
        open={snapshot.createPanelOpen}
        mode={snapshot.createOfferDrawerMode}
        locationSubtitle={snapshot.locationSubtitle}
        draft={snapshot.createOfferDraft}
        canConfirm={snapshot.canConfirmCreateOffer}
        saveGated={false}
        status={snapshot.createOfferStatus}
        error={snapshot.createOfferError}
        onOpenChange={(nextOpen) => {
          if (!nextOpen) {
            onCloseCreatePanel()
          }
        }}
        onPatch={onPatchCreateOfferDraft}
        onConfirm={onConfirmCreateOffer}
      />
    </>
  )
}
