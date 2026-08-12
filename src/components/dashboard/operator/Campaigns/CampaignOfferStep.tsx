import type { LucideIcon } from "lucide-react"
import {
  BanIcon,
  BanknoteIcon,
  RefreshCwIcon,
  SearchIcon,
  SquarePenIcon,
  TagIcon,
  TicketPercentIcon,
} from "lucide-react"

import { CreateEditOfferDrawer } from "@/components/dashboard/operator/Offers/CreateEditOfferDrawer"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import type { CampaignCatalogOfferDetailsDraft } from "@/lib/operatorOffers/offerCatalogPresentation"
import {
  CAMPAIGN_OFFER_COPY,
  type CampaignOfferStanceId,
} from "@/lib/operatorCampaigns/campaignOfferPresentation"
import type { CampaignExistingOfferPickerCard } from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"
import type {
  CampaignExistingOfferPickerViewModel,
  CampaignOfferOptionViewModel,
  CampaignOfferViewModel,
} from "@/lib/operatorCampaigns/createCampaignWizardModule"
import {
  operatorDashboardOfferDetailsPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import { cn } from "@/lib/utils"

const OFFER_STANCE_ICONS: Record<CampaignOfferStanceId, LucideIcon> = {
  "no-offer": BanIcon,
  "existing-offer": TagIcon,
  "create-new-offer": SquarePenIcon,
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

type CampaignOfferStepProps = {
  offer: CampaignOfferViewModel
  dashboardMode: OperatorDashboardMode
  locationId: number | null
  onSelectStance: (stanceId: CampaignOfferStanceId) => void
  onCloseCreatePanel: () => void
  onEditAttachedOffer: () => void
  onPatchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  onConfirmCreateOffer: () => void
  onExistingOfferSearchChange: (query: string) => void
  onSelectExistingOffer: (offerId: number) => void
  onRetryExistingOfferPicker: () => void
  onCreateNewOfferFromPicker: () => void
}

function OfferStanceCard({
  option,
  onSelect,
}: {
  option: CampaignOfferOptionViewModel
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
        "h-auto min-h-0 w-full items-center justify-start gap-2.5 rounded-[4px] border px-[18px] py-4 text-left whitespace-normal hover:bg-transparent",
        option.disabled
          ? "cursor-not-allowed border-op-card-border bg-op-background-secondary opacity-70"
          : option.selected
            ? "border-[var(--op-color-gray-550)] bg-op-background-primary"
            : "border-op-card-border bg-op-background-primary hover:border-[var(--op-color-gray-550)]"
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
}: {
  title: string
  onEdit: () => void
}) {
  return (
    <div className="flex w-full items-start justify-between gap-3 rounded-[4px] border border-op-card-border bg-op-background-secondary px-4 py-3">
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
        onClick={onEdit}
      >
        {CAMPAIGN_OFFER_COPY.attachedSummaryEdit}
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
}: {
  card: CampaignExistingOfferPickerCard
  selectLabel: string
  viewDetailsLabel: string
  viewDetailsHref: string | null
  onSelect: () => void
}) {
  const Icon = PICKER_TYPE_ICONS[card.offerTypeIconId]

  return (
    <div className="flex w-full flex-col gap-3 rounded-[4px] border border-op-card-border bg-op-background-primary px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex min-w-0 items-start gap-2.5">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-[2px] bg-op-background-secondary p-2.5">
          <Icon className="size-4 text-op-text-primary" aria-hidden />
        </span>
        <div className="flex min-w-0 flex-col gap-1">
          <p className="m-0 text-sm font-medium text-op-text-primary">
            {card.title}
          </p>
          <p className="m-0 text-xs font-medium leading-normal text-[var(--op-color-gray-550)]">
            {card.metaLine}
          </p>
        </div>
      </div>
      <div className="flex shrink-0 items-center gap-2">
        {viewDetailsHref != null ? (
          <Button asChild variant="op-tertiary" className="h-8 px-3">
            <a
              href={viewDetailsHref}
              target="_blank"
              rel="noopener noreferrer"
            >
              {viewDetailsLabel}
            </a>
          </Button>
        ) : null}
        <Button
          type="button"
          variant="op-secondary"
          className="h-8 px-3"
          onClick={onSelect}
        >
          {selectLabel}
        </Button>
      </div>
    </div>
  )
}

function ExistingOfferPicker({
  picker,
  dashboardMode,
  locationId,
  onSearchChange,
  onSelect,
  onRetry,
  onCreateNew,
}: {
  picker: CampaignExistingOfferPickerViewModel
  dashboardMode: OperatorDashboardMode
  locationId: number | null
  onSearchChange: (query: string) => void
  onSelect: (offerId: number) => void
  onRetry: () => void
  onCreateNew: () => void
}) {
  return (
    <div
      className="flex w-full flex-col gap-4"
      data-testid="campaign-existing-offer-picker"
    >
      <div className="relative w-full">
        <SearchIcon
          className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-[var(--op-color-gray-550)]"
          aria-hidden
        />
        <Input
          value={picker.searchQuery}
          onChange={(event) => {
            onSearchChange(event.target.value)
          }}
          placeholder={picker.searchPlaceholder}
          className="h-10 border-op-card-border bg-op-background-primary pl-9"
          aria-label={picker.searchPlaceholder}
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
            onClick={onRetry}
          >
            {picker.retryLabel}
          </Button>
        </div>
      ) : null}

      {picker.loadStatus === "ready" && picker.isEmpty ? (
        <div className="flex flex-col gap-3">
          <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {picker.emptyHelper}
          </p>
          {picker.createNewOfferLabel != null ? (
            <Button
              type="button"
              variant="op-secondary"
              className="w-fit"
              onClick={onCreateNew}
            >
              {picker.createNewOfferLabel}
            </Button>
          ) : null}
        </div>
      ) : null}

      {picker.loadStatus === "ready" && !picker.isEmpty ? (
        <div className="flex w-full flex-col gap-3">
          {picker.cards.map((card) => (
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
              onSelect={() => {
                onSelect(card.id)
              }}
            />
          ))}
        </div>
      ) : null}
    </div>
  )
}

function EstimatedUsageSummary({
  offer,
}: {
  offer: CampaignOfferViewModel
}) {
  const { usageSummary } = offer

  return (
    <aside
      className="flex w-full shrink-0 flex-col gap-6 rounded-[4px] border border-op-card-border bg-op-background-primary p-5 lg:w-[min(100%,560px)]"
      aria-label={usageSummary.title}
    >
      <div className="flex flex-col gap-2">
        <h3 className="m-0 text-lg font-semibold leading-normal text-op-text-primary">
          {usageSummary.title}
        </h3>
        <p className="m-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]">
          {usageSummary.audienceLine}
        </p>
      </div>
      <dl className="m-0 flex w-full flex-col gap-3.5">
        {usageSummary.rows.map((row, index) => (
          <div key={row.label} className="flex flex-col gap-3.5">
            <div className="flex items-center justify-between gap-3 text-sm">
              <dt className="m-0 font-semibold text-[var(--op-color-gray-550)]">
                {row.label}
              </dt>
              <dd className="m-0 font-medium text-op-text-primary">{row.value}</dd>
            </div>
            {index < usageSummary.rows.length - 1 ? (
              <div className="h-px w-full bg-op-card-border" aria-hidden />
            ) : null}
          </div>
        ))}
      </dl>
    </aside>
  )
}

/**
 * Campaign wizard Offer step — Figma 4730:53493 / tickets 22 + 18 + 30.
 * Existing offer inline picker; Create a new offer opens shared drawer.
 */
export function CampaignOfferStep({
  offer,
  dashboardMode,
  locationId,
  onSelectStance,
  onCloseCreatePanel,
  onEditAttachedOffer,
  onPatchCreateOfferDraft,
  onConfirmCreateOffer,
  onExistingOfferSearchChange,
  onSelectExistingOffer,
  onRetryExistingOfferPicker,
  onCreateNewOfferFromPicker,
}: CampaignOfferStepProps) {
  const picker = offer.existingOfferPicker
  const showAttachedSummary =
    offer.attachedOfferId != null && picker == null

  return (
    <>
      <div className="flex w-full flex-col items-start justify-between gap-8 lg:flex-row lg:gap-[42px]">
        <div className="flex min-h-0 w-full max-w-[690px] flex-col gap-7">
          <header className="flex flex-col gap-2">
            <h2 className="m-0 text-xl font-semibold leading-normal text-op-text-primary sm:text-[22px]">
              {offer.stepHeading}
            </h2>
            <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
              {offer.stepDescription}
            </p>
          </header>

          <div
            className="flex w-full flex-col gap-[18px]"
            role="radiogroup"
            aria-label={offer.stepHeading}
          >
            {offer.options.map((option) => (
              <OfferStanceCard
                key={option.id}
                option={option}
                onSelect={() => {
                  if (!option.disabled) {
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
              locationId={locationId}
              onSearchChange={onExistingOfferSearchChange}
              onSelect={onSelectExistingOffer}
              onRetry={onRetryExistingOfferPicker}
              onCreateNew={onCreateNewOfferFromPicker}
            />
          ) : null}

          {showAttachedSummary ? (
            <AttachedOfferSummary
              title={
                offer.attachedOfferTitle
                ?? CAMPAIGN_OFFER_COPY.attachedSummaryFallbackTitle
              }
              onEdit={onEditAttachedOffer}
            />
          ) : null}
        </div>

        <EstimatedUsageSummary offer={offer} />
      </div>

      <CreateEditOfferDrawer
        open={offer.createPanelOpen}
        mode={offer.createOfferDrawerMode}
        locationSubtitle={offer.locationSubtitle}
        draft={offer.createOfferDraft}
        canConfirm={offer.canConfirmCreateOffer}
        saveGated={offer.createOfferSaveGated}
        status={offer.createOfferStatus}
        error={offer.createOfferError}
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
