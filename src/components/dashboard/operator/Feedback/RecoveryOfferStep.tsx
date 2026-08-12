import type { LucideIcon } from "lucide-react"
import { SquarePenIcon, TagIcon } from "lucide-react"

import { CreateEditOfferDrawer } from "@/components/dashboard/operator/Offers/CreateEditOfferDrawer"
import { Button } from "@/components/ui/button"
import type { RespondWithRecoveryOfferSnapshot } from "@/lib/operatorFeedback/createRespondWithRecoveryOfferModule"
import {
  RECOVERY_OFFER_STEP_COPY,
  type RecoveryOfferStanceId,
  type RecoveryOfferStanceOptionViewModel,
} from "@/lib/operatorFeedback/recoveryOfferPresentation"
import type { CampaignCatalogOfferDetailsDraft } from "@/lib/operatorOffers/offerCatalogPresentation"
import { cn } from "@/lib/utils"

const OFFER_STANCE_ICONS: Record<RecoveryOfferStanceId, LucideIcon> = {
  "create-and-select": SquarePenIcon,
  "existing-offer": TagIcon,
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
  disabled,
}: {
  title: string
  onEdit: () => void
  disabled?: boolean
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
        disabled={disabled}
        onClick={onEdit}
      >
        {RECOVERY_OFFER_STEP_COPY.attachedSummaryEdit}
      </Button>
    </div>
  )
}

/**
 * Recovery Offer step — Create and select (ticket 04). Existing stays disabled.
 */
export function RecoveryOfferStep({
  snapshot,
  disabled = false,
  onSelectStance,
  onCloseCreatePanel,
  onEditAttachedOffer,
  onPatchCreateOfferDraft,
  onConfirmCreateOffer,
}: RecoveryOfferStepProps) {
  const showAttachedSummary = snapshot.offerId != null

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
