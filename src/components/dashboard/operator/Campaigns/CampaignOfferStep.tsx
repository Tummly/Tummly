import type { LucideIcon } from "lucide-react"
import { BanIcon, SquarePenIcon, TagIcon } from "lucide-react"

import { CampaignCreateOfferPanel } from "@/components/dashboard/operator/Campaigns/CampaignCreateOfferPanel"
import { Button } from "@/components/ui/button"
import type { CampaignCatalogOfferDetailsDraft } from "@/lib/operatorCampaigns/campaignOfferCatalogPresentation"
import {
  CAMPAIGN_OFFER_COPY,
  type CampaignOfferStanceId,
} from "@/lib/operatorCampaigns/campaignOfferPresentation"
import type {
  CampaignOfferOptionViewModel,
  CampaignOfferViewModel,
} from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { cn } from "@/lib/utils"

const OFFER_STANCE_ICONS: Record<CampaignOfferStanceId, LucideIcon> = {
  "no-offer": BanIcon,
  "existing-offer": TagIcon,
  "create-new-offer": SquarePenIcon,
}

type CampaignOfferStepProps = {
  offer: CampaignOfferViewModel
  onSelectStance: (stanceId: CampaignOfferStanceId) => void
  onCloseCreatePanel: () => void
  onEditAttachedOffer: () => void
  onPatchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  onConfirmCreateOffer: () => void
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
 * Campaign wizard Offer step — Figma 4730:53493 / ticket 22.
 * No offer clears attach; Create and select opens side panel; Existing disabled.
 */
export function CampaignOfferStep({
  offer,
  onSelectStance,
  onCloseCreatePanel,
  onEditAttachedOffer,
  onPatchCreateOfferDraft,
  onConfirmCreateOffer,
}: CampaignOfferStepProps) {
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

          {offer.attachedOfferId != null ? (
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

      <CampaignCreateOfferPanel
        open={offer.createPanelOpen}
        draft={offer.createOfferDraft}
        canConfirm={offer.canConfirmCreateOffer}
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
