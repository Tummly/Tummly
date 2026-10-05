import { useEffect, useState } from "react"
import { XIcon } from "lucide-react"

import { fetchShopOfferCardPreview } from "@/api/shopOfferCardOfferApi"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import {
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_INPUT_CLASS,
  FEEDBACK_RECOVERY_SELECT_MENU_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import {
  CAMPAIGN_CATALOG_OFFER_PURCHASE_REQUIREMENT_OPTIONS,
  CAMPAIGN_CATALOG_OFFER_TYPE_OPTIONS,
  CAMPAIGN_CATALOG_OFFER_VALIDITY_OPTIONS,
  CAMPAIGN_OFFER_ADDITIONAL_EXCLUSIONS_MAX,
  CAMPAIGN_OFFER_DESCRIPTION_MAX,
  CAMPAIGN_OFFER_TITLE_MAX,
  type CampaignCatalogOfferDetailsDraft,
  type CampaignCatalogOfferPurchaseRequirementId,
  type CampaignCatalogOfferTypeId,
  type CampaignCatalogOfferValidityId,
} from "@/lib/operatorOffers/offerCatalogPresentation"
import type { ShopOfferCardSetupSnapshot } from "@/lib/operatorShop/createShopOfferCardSetupModule"
import {
  SHOP_OFFER_CARD_PREVIEW_ASPECT,
  SHOP_OFFER_CARD_SETUP_COPY,
} from "@/lib/operatorShop/shopOfferCardSetupPresentation"
import { cn } from "@/lib/utils"

export type ShopOfferCardSetupDialogProps = {
  snapshot: ShopOfferCardSetupSnapshot
  locationId: number | null
  onOpenChange: (open: boolean) => void
  onOfferTypeChange: (offerType: CampaignCatalogOfferTypeId | null) => void
  onPatchDraft: (patch: Partial<CampaignCatalogOfferDetailsDraft>) => void
  onContinue: () => void
  onCancel: () => void
}

const FIELD_INPUT_CLASS = cn(FEEDBACK_INPUT_CLASS, "h-auto min-h-12 py-[15px]")

function TypeSpecificFields({
  draft,
  saving,
  onPatchDraft,
}: {
  draft: CampaignCatalogOfferDetailsDraft
  saving: boolean
  onPatchDraft: ShopOfferCardSetupDialogProps["onPatchDraft"]
}) {
  if (draft.offerType === "percentage_discount") {
    return (
      <div className="flex w-full flex-col gap-2">
        <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
          {SHOP_OFFER_CARD_SETUP_COPY.discountPercentageLabel}
        </Label>
        <Input
          type="text"
          inputMode="decimal"
          disabled={saving}
          value={draft.discountPercentage}
          onChange={(event) =>
            onPatchDraft({ discountPercentage: event.target.value })
          }
          className={FIELD_INPUT_CLASS}
          placeholder="20"
        />
      </div>
    )
  }

  if (draft.offerType === "fixed_discount") {
    return (
      <div className="flex w-full flex-col gap-2">
        <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
          {SHOP_OFFER_CARD_SETUP_COPY.discountAmountLabel}
        </Label>
        <Input
          type="text"
          inputMode="decimal"
          disabled={saving}
          value={draft.discountAmount}
          onChange={(event) =>
            onPatchDraft({ discountAmount: event.target.value })
          }
          className={FIELD_INPUT_CLASS}
          placeholder="5"
        />
      </div>
    )
  }

  if (draft.offerType === "free_item") {
    return (
      <div className="flex w-full flex-col gap-[18px]">
        <div className="flex w-full flex-col gap-2">
          <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
            {SHOP_OFFER_CARD_SETUP_COPY.freeItemLabel}
          </Label>
          <Input
            type="text"
            disabled={saving}
            value={draft.freeItemText}
            onChange={(event) =>
              onPatchDraft({ freeItemText: event.target.value })
            }
            className={FIELD_INPUT_CLASS}
            placeholder="Dessert"
          />
        </div>
        <FloatingLabelSelect
          label={SHOP_OFFER_CARD_SETUP_COPY.purchaseRequirementLabel}
          options={CAMPAIGN_CATALOG_OFFER_PURCHASE_REQUIREMENT_OPTIONS.map(
            (option) => ({
              value: option.id,
              label: option.label,
            })
          )}
          value={draft.purchaseRequirement ?? ""}
          onValueChange={(value) =>
            onPatchDraft({
              purchaseRequirement:
                value as CampaignCatalogOfferPurchaseRequirementId,
            })
          }
          disabled={saving}
          disableFocusRing
          contentClassName={cn(FEEDBACK_RECOVERY_SELECT_MENU_CLASS, "z-[150]")}
          itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
        />
        {draft.purchaseRequirement === "with_minimum_spend" ? (
          <div className="flex w-full flex-col gap-2">
            <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
              {SHOP_OFFER_CARD_SETUP_COPY.minimumSpendLabel}
            </Label>
            <Input
              type="text"
              inputMode="decimal"
              disabled={saving}
              value={draft.minimumSpend}
              onChange={(event) =>
                onPatchDraft({ minimumSpend: event.target.value })
              }
              className={FIELD_INPUT_CLASS}
              placeholder="10"
            />
          </div>
        ) : null}
        <div className="flex w-full flex-col gap-2">
          <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
            {SHOP_OFFER_CARD_SETUP_COPY.additionalExclusionsLabel}
          </Label>
          <Textarea
            disabled={saving}
            value={draft.additionalExclusions}
            maxLength={CAMPAIGN_OFFER_ADDITIONAL_EXCLUSIONS_MAX}
            onChange={(event) =>
              onPatchDraft({ additionalExclusions: event.target.value })
            }
            className={cn(FEEDBACK_TEXTAREA_CLASS, "min-h-[80px]")}
          />
        </div>
      </div>
    )
  }

  if (draft.offerType === "replacement_item") {
    return (
      <div className="flex w-full flex-col gap-2">
        <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
          {SHOP_OFFER_CARD_SETUP_COPY.replacementItemLabel}
        </Label>
        <Input
          type="text"
          disabled={saving}
          value={draft.replacementItemText}
          onChange={(event) =>
            onPatchDraft({ replacementItemText: event.target.value })
          }
          className={FIELD_INPUT_CLASS}
          placeholder="Main course"
        />
      </div>
    )
  }

  return null
}

function OfferCardPrintPreview({
  locationId,
  title,
  active,
}: {
  locationId: number | null
  title: string
  active: boolean
}) {
  const headline = title.trim() || SHOP_OFFER_CARD_SETUP_COPY.titlePlaceholder
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [previewStatus, setPreviewStatus] = useState<
    "idle" | "loading" | "ready" | "error"
  >("idle")

  useEffect(() => {
    if (!active || locationId == null) {
      return
    }

    let cancelled = false
    setPreviewStatus("loading")

    const timer = window.setTimeout(() => {
      void (async () => {
        try {
          const blob = await fetchShopOfferCardPreview(locationId, headline)
          if (cancelled) {
            return
          }
          if (!blob.type.startsWith("image/") && blob.size < 100) {
            throw new Error("preview_not_image")
          }
          const nextUrl = URL.createObjectURL(blob)
          if (cancelled) {
            URL.revokeObjectURL(nextUrl)
            return
          }
          setPreviewUrl((previous) => {
            if (previous != null) {
              URL.revokeObjectURL(previous)
            }
            return nextUrl
          })
          setPreviewStatus("ready")
        } catch {
          if (!cancelled) {
            setPreviewStatus("error")
          }
        }
      })()
    }, 250)

    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [active, headline, locationId])

  useEffect(() => {
    return () => {
      if (previewUrl != null) {
        URL.revokeObjectURL(previewUrl)
      }
    }
  }, [previewUrl])

  return (
    <div
      className="relative w-full max-w-[480px] overflow-hidden rounded-[2px] bg-op-surface-secondary shadow-sm"
      style={{ aspectRatio: SHOP_OFFER_CARD_PREVIEW_ASPECT }}
      aria-label={SHOP_OFFER_CARD_SETUP_COPY.previewLabel}
    >
      {previewUrl != null && previewStatus === "ready" ? (
        <img
          src={previewUrl}
          alt=""
          className="pointer-events-none absolute inset-0 size-full object-contain"
        />
      ) : null}
      {previewStatus === "loading" ? (
        <div
          className="absolute inset-0 animate-pulse bg-op-surface-secondary"
          aria-hidden
        />
      ) : null}
      {previewStatus === "error" ? (
        <div className="absolute inset-0 flex items-center justify-center p-4">
          <p className="m-0 text-center font-sans text-xs text-op-text-danger">
            {SHOP_OFFER_CARD_SETUP_COPY.previewError}
          </p>
        </div>
      ) : null}
    </div>
  )
}

export function ShopOfferCardSetupDialog({
  snapshot,
  locationId,
  onOpenChange,
  onOfferTypeChange,
  onPatchDraft,
  onContinue,
  onCancel,
}: ShopOfferCardSetupDialogProps) {
  const saving = snapshot.saveStatus === "saving"
  const showDetails = snapshot.draft.offerType != null

  return (
    <Dialog open={snapshot.isOpen} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className={cn(
          "z-[140] gap-0 overflow-hidden border-0 bg-transparent p-0 text-op-text-primary shadow-none sm:max-w-[1424px]"
        )}
      >
        <div className="flex max-h-[90vh] w-full flex-col overflow-hidden rounded-[4px] bg-op-surface-primary sm:flex-row">
          <div className="flex min-h-0 flex-1 flex-col justify-between gap-10 overflow-y-auto p-8">
            <div className="flex flex-col gap-10">
              <div className="flex flex-col gap-3">
                <DialogTitle className="pr-0 font-jakarta text-2xl font-bold leading-normal text-op-text-primary">
                  {SHOP_OFFER_CARD_SETUP_COPY.title}
                </DialogTitle>
                <DialogDescription className="font-sans text-base font-medium text-[var(--op-color-gray-550)]">
                  {SHOP_OFFER_CARD_SETUP_COPY.subtitle}
                </DialogDescription>
              </div>

              <div className="flex flex-col gap-5">
                <FloatingLabelSelect
                  label={SHOP_OFFER_CARD_SETUP_COPY.offerTypeLabel}
                  options={CAMPAIGN_CATALOG_OFFER_TYPE_OPTIONS.map(
                    (option) => ({
                      value: option.id,
                      label: option.label,
                    })
                  )}
                  value={snapshot.draft.offerType ?? ""}
                  onValueChange={(value) =>
                    onOfferTypeChange(
                      (value as CampaignCatalogOfferTypeId) || null
                    )
                  }
                  disabled={saving}
                  disableFocusRing
                  contentClassName={cn(
                    FEEDBACK_RECOVERY_SELECT_MENU_CLASS,
                    "z-[150]"
                  )}
                  itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
                />

                {showDetails ? (
                  <>
                    <TypeSpecificFields
                      draft={snapshot.draft}
                      saving={saving}
                      onPatchDraft={onPatchDraft}
                    />

                    <div className="h-px w-full bg-op-border-default" />

                    <div className="flex w-full flex-col gap-2">
                      <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
                        {SHOP_OFFER_CARD_SETUP_COPY.titleLabel}
                      </Label>
                      <Input
                        type="text"
                        disabled={saving}
                        value={snapshot.draft.title}
                        maxLength={CAMPAIGN_OFFER_TITLE_MAX}
                        onChange={(event) =>
                          onPatchDraft({
                            title: event.target.value,
                            titleTouched: true,
                          })
                        }
                        className={FIELD_INPUT_CLASS}
                        placeholder={
                          SHOP_OFFER_CARD_SETUP_COPY.titlePlaceholder
                        }
                      />
                    </div>

                    <div className="flex w-full flex-col gap-2">
                      <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
                        {SHOP_OFFER_CARD_SETUP_COPY.descriptionLabel}
                      </Label>
                      <Textarea
                        disabled={saving}
                        value={snapshot.draft.description}
                        maxLength={CAMPAIGN_OFFER_DESCRIPTION_MAX}
                        onChange={(event) =>
                          onPatchDraft({ description: event.target.value })
                        }
                        className={cn(FEEDBACK_TEXTAREA_CLASS, "min-h-[96px]")}
                        placeholder={
                          SHOP_OFFER_CARD_SETUP_COPY.descriptionPlaceholder
                        }
                      />
                    </div>

                    <FloatingLabelSelect
                      label={SHOP_OFFER_CARD_SETUP_COPY.validityLabel}
                      options={CAMPAIGN_CATALOG_OFFER_VALIDITY_OPTIONS.map(
                        (option) => ({
                          value: option.id,
                          label: option.label,
                        })
                      )}
                      value={snapshot.draft.validity}
                      onValueChange={(value) =>
                        onPatchDraft({
                          validity: value as CampaignCatalogOfferValidityId,
                        })
                      }
                      disabled={saving}
                      disableFocusRing
                      contentClassName={cn(
                        FEEDBACK_RECOVERY_SELECT_MENU_CLASS,
                        "z-[150]"
                      )}
                      itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
                    />

                    {snapshot.draft.validity === "choose_expiry_date" ? (
                      <div className="flex w-full flex-col gap-2">
                        <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
                          {SHOP_OFFER_CARD_SETUP_COPY.expiryDateLabel}
                        </Label>
                        <Input
                          type="date"
                          disabled={saving}
                          value={snapshot.draft.expiryDate}
                          onChange={(event) =>
                            onPatchDraft({ expiryDate: event.target.value })
                          }
                          className={FIELD_INPUT_CLASS}
                        />
                      </div>
                    ) : null}

                    <div className="flex w-full flex-col gap-2">
                      <Label className={FEEDBACK_FIELD_LABEL_CLASS}>
                        {SHOP_OFFER_CARD_SETUP_COPY.staffInstructionsLabel}
                      </Label>
                      <Textarea
                        disabled={saving}
                        value={snapshot.draft.staffInstructions}
                        onChange={(event) =>
                          onPatchDraft({
                            staffInstructions: event.target.value,
                          })
                        }
                        className={cn(FEEDBACK_TEXTAREA_CLASS, "min-h-[80px]")}
                      />
                    </div>
                  </>
                ) : null}

                <div className="h-px w-full bg-op-border-default" />

                <div className="flex flex-col gap-1.5">
                  <p className="m-0 font-sans text-base font-medium text-op-text-primary">
                    {SHOP_OFFER_CARD_SETUP_COPY.importantTitle}
                  </p>
                  <p className="m-0 max-w-[531px] font-sans text-sm text-[var(--op-color-gray-550)]">
                    {SHOP_OFFER_CARD_SETUP_COPY.importantBody}
                  </p>
                </div>

                {snapshot.saveError != null ? (
                  <p className="m-0 font-sans text-sm text-op-text-danger">
                    {snapshot.saveError}
                  </p>
                ) : null}
              </div>
            </div>

            <div className="flex flex-wrap gap-3">
              <Button
                type="button"
                variant="op-primary"
                disabled={!snapshot.canConfirm || saving}
                onClick={onContinue}
              >
                {SHOP_OFFER_CARD_SETUP_COPY.continueLabel}
              </Button>
              <Button
                type="button"
                variant="op-secondary"
                disabled={saving}
                onClick={onCancel}
              >
                {SHOP_OFFER_CARD_SETUP_COPY.cancelLabel}
              </Button>
            </div>
          </div>

          <div className="relative flex w-full shrink-0 flex-col items-center justify-center gap-8 bg-op-surface-secondary p-8 sm:w-[639px]">
            <Button
              type="button"
              variant="op-collapse"
              size="icon"
              className="absolute top-8 right-8"
              onClick={onCancel}
              aria-label="Close"
            >
              <XIcon className="size-[18px]" aria-hidden />
            </Button>
            <p className="m-0 font-sans text-base font-medium text-[var(--op-color-gray-550)]">
              {SHOP_OFFER_CARD_SETUP_COPY.previewLabel}
            </p>
            <OfferCardPrintPreview
              locationId={locationId}
              title={snapshot.draft.title}
              active={snapshot.isOpen}
            />
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
