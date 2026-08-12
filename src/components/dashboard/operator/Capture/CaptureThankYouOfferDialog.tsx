"use client"

import {
  BanIcon,
  SearchIcon,
  SquarePenIcon,
  TagIcon,
} from "lucide-react"

import { CreateEditOfferDrawer } from "@/components/dashboard/operator/Offers/CreateEditOfferDrawer"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { CAMPAIGN_EXISTING_OFFER_PICKER_COPY } from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"
import {
  CAPTURE_THANK_YOU_OFFER_COPY,
} from "@/lib/operatorCapture/captureThankYouOfferPresentation"
import type { CaptureThankYouOfferDialogSnapshot } from "@/lib/operatorCapture/createCaptureThankYouOfferModule"
import type { CampaignCatalogOfferDetailsDraft } from "@/lib/operatorOffers/offerCatalogPresentation"
import { cn } from "@/lib/utils"

type CaptureThankYouOfferDialogProps = {
  dialog: CaptureThankYouOfferDialogSnapshot
  locationName: string
  onOpenChange: (open: boolean) => void
  onSelectCreate: () => void
  onSelectExisting: () => void
  onClear: () => void
  onBackToStances: () => void
  onPatchCreateDraft: (patch: Partial<CampaignCatalogOfferDetailsDraft>) => void
  onConfirmCreate: () => void
  onExistingSearchChange: (query: string) => void
  onSelectExistingOffer: (offerId: number) => void
  onRetryExisting: () => void
}

function StanceButton({
  title,
  description,
  icon: Icon,
  onClick,
}: {
  title: string
  description: string
  icon: typeof TagIcon
  onClick: () => void
}) {
  return (
    <Button
      type="button"
      variant="ghost"
      className="h-auto min-h-0 w-full items-center justify-start gap-2.5 rounded-[4px] border border-op-card-border bg-op-background-primary px-[18px] py-4 text-left whitespace-normal hover:border-[var(--op-color-gray-550)] hover:bg-transparent"
      onClick={onClick}
    >
      <span className="flex size-9 shrink-0 items-center justify-center rounded-[2px] bg-op-background-secondary p-2.5">
        <Icon className="size-4 text-op-text-primary" aria-hidden />
      </span>
      <span className="flex min-w-0 flex-col gap-1">
        <span className="text-sm font-medium leading-normal text-op-text-primary">
          {title}
        </span>
        <span className="text-xs font-medium leading-normal text-[var(--op-color-gray-550)]">
          {description}
        </span>
      </span>
    </Button>
  )
}

/** Capture Guest experience thank-you attach — Create / Existing / clear. */
export function CaptureThankYouOfferDialog({
  dialog,
  locationName,
  onOpenChange,
  onSelectCreate,
  onSelectExisting,
  onClear,
  onBackToStances,
  onPatchCreateDraft,
  onConfirmCreate,
  onExistingSearchChange,
  onSelectExistingOffer,
  onRetryExisting,
}: CaptureThankYouOfferDialogProps) {
  const copy = CAPTURE_THANK_YOU_OFFER_COPY
  const attachedTitle = dialog.attached.title?.trim() ?? ""

  return (
    <>
      <Dialog
        open={dialog.isOpen && dialog.panel !== "create"}
        onOpenChange={onOpenChange}
      >
        <DialogContent className="z-[140] max-h-[90vh] overflow-y-auto sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{copy.dialogTitle}</DialogTitle>
            <DialogDescription>{copy.dialogDescription}</DialogDescription>
          </DialogHeader>

          {attachedTitle.length > 0 ? (
            <div className="rounded-[4px] border border-op-card-border bg-op-background-secondary px-4 py-3">
              <p className="m-0 text-xs font-medium text-[var(--op-color-gray-550)]">
                {copy.attachedLabel}
              </p>
              <p className="m-0 mt-1 text-sm font-medium text-op-text-primary">
                {attachedTitle}
              </p>
              {!dialog.attached.live ? (
                <p className="m-0 mt-1 text-xs text-op-text-secondary">
                  {copy.notLiveHelper}
                </p>
              ) : null}
            </div>
          ) : null}

          {dialog.panel === "stances" ? (
            <div className="flex flex-col gap-2.5">
              <StanceButton
                title={copy.createStanceTitle}
                description={copy.createStanceDescription}
                icon={SquarePenIcon}
                onClick={onSelectCreate}
              />
              <StanceButton
                title={copy.existingStanceTitle}
                description={copy.existingStanceDescription}
                icon={TagIcon}
                onClick={onSelectExisting}
              />
              {dialog.attached.offerId != null ? (
                <StanceButton
                  title={copy.clearStanceTitle}
                  description={copy.clearStanceDescription}
                  icon={BanIcon}
                  onClick={onClear}
                />
              ) : null}
              <Button
                type="button"
                variant="op-tertiary"
                onClick={() => {
                  onOpenChange(false)
                }}
              >
                {copy.closeLabel}
              </Button>
            </div>
          ) : null}

          {dialog.panel === "existing" ? (
            <div className="flex flex-col gap-3">
              <div className="relative">
                <SearchIcon
                  className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-op-text-secondary"
                  aria-hidden
                />
                <Input
                  value={dialog.existingPickerSearchQuery}
                  onChange={(event) => {
                    onExistingSearchChange(event.target.value)
                  }}
                  placeholder={CAMPAIGN_EXISTING_OFFER_PICKER_COPY.searchPlaceholder}
                  className="pl-9"
                />
              </div>
              {dialog.existingPickerLoadStatus === "loading" ? (
                <p className="m-0 text-sm text-op-text-secondary">Loading…</p>
              ) : null}
              {dialog.existingPickerLoadStatus === "error" ? (
                <div className="flex flex-col gap-2">
                  <p className="m-0 text-sm text-op-text-secondary">
                    {dialog.existingPickerError
                      ?? CAMPAIGN_EXISTING_OFFER_PICKER_COPY.loadError}
                  </p>
                  <Button type="button" variant="op-secondary" onClick={onRetryExisting}>
                    {CAMPAIGN_EXISTING_OFFER_PICKER_COPY.retryLabel}
                  </Button>
                </div>
              ) : null}
              {dialog.existingPickerLoadStatus === "ready" ? (
                <div className="flex max-h-72 flex-col gap-2 overflow-y-auto">
                  {dialog.existingPickerCards.map((card) => (
                    <div
                      key={card.id}
                      className={cn(
                        "flex items-start justify-between gap-3 rounded-[4px] border border-op-card-border px-3 py-3"
                      )}
                    >
                      <div className="min-w-0">
                        <p className="m-0 text-sm font-medium text-op-text-primary">
                          {card.title}
                        </p>
                        <p className="m-0 mt-1 text-xs text-op-text-secondary">
                          {card.metaLine}
                        </p>
                      </div>
                      <Button
                        type="button"
                        variant="op-secondary"
                        onClick={() => {
                          onSelectExistingOffer(card.id)
                        }}
                      >
                        {CAMPAIGN_EXISTING_OFFER_PICKER_COPY.selectLabel}
                      </Button>
                    </div>
                  ))}
                  {dialog.existingPickerEmptyHelper != null ? (
                    <p className="m-0 text-sm text-op-text-secondary">
                      {dialog.existingPickerEmptyHelper}
                    </p>
                  ) : null}
                </div>
              ) : null}
              <Button type="button" variant="op-tertiary" onClick={onBackToStances}>
                Back
              </Button>
            </div>
          ) : null}
        </DialogContent>
      </Dialog>

      <CreateEditOfferDrawer
        open={dialog.isOpen && dialog.panel === "create"}
        mode="create"
        locationSubtitle={locationName}
        draft={dialog.createOfferDraft}
        canConfirm={dialog.canConfirmCreateOffer}
        saveGated={false}
        status={dialog.createOfferStatus}
        error={dialog.createOfferError}
        onOpenChange={(open) => {
          if (!open) {
            onBackToStances()
          }
        }}
        onPatch={onPatchCreateDraft}
        onConfirm={onConfirmCreate}
      />
    </>
  )
}
