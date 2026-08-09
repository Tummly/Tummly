"use client"

import { Loader2Icon, XIcon } from "lucide-react"

import { RecoveryOfferPurchaseRequirementCards } from "@/components/dashboard/operator/Feedback/RecoveryOfferPurchaseRequirementCards"
import { RecoveryOfferTypeCards } from "@/components/dashboard/operator/Feedback/RecoveryOfferTypeCards"
import {
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_INPUT_CLASS,
  FEEDBACK_RECOVERY_SELECT_MENU_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import {
  CAMPAIGN_CATALOG_OFFER_VALIDITY_OPTIONS,
  CAMPAIGN_OFFER_DESCRIPTION_MAX,
  CAMPAIGN_OFFER_TITLE_MAX,
  type CampaignCatalogOfferDetailsDraft,
  type CampaignCatalogOfferTypeId,
  type CampaignCatalogOfferValidityId,
} from "@/lib/operatorCampaigns/campaignOfferCatalogPresentation"
import { CAMPAIGN_OFFER_COPY } from "@/lib/operatorCampaigns/campaignOfferPresentation"
import {
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import type { RecoveryOfferPurchaseRequirementId } from "@/lib/operatorFeedback/recoveryOfferPresentation"
import type { RecoveryOfferTypeId } from "@/lib/operatorFeedback/recoveryOfferPresentation"
import { cn } from "@/lib/utils"

type CampaignCreateOfferPanelProps = {
  open: boolean
  draft: CampaignCatalogOfferDetailsDraft
  canConfirm: boolean
  status: "idle" | "saving" | "error"
  error: string | null
  onOpenChange: (open: boolean) => void
  onPatch: (patch: Partial<CampaignCatalogOfferDetailsDraft>) => void
  onConfirm: () => void
}

/**
 * Side panel to create an Active Offers catalog definition and select it
 * on the Campaign Offer step (ticket 22). Field chrome mirrors Recovery offer
 * form patterns; POST goes to /api/offers — not Feedback recovery APIs.
 */
export function CampaignCreateOfferPanel({
  open,
  draft,
  canConfirm,
  status,
  error,
  onOpenChange,
  onPatch,
  onConfirm,
}: CampaignCreateOfferPanelProps) {
  const saving = status === "saving"

  return (
    <Drawer
      direction="right"
      open={open}
      onOpenChange={onOpenChange}
      shouldScaleBackground={false}
    >
      <DrawerContent
        /* Above RecoveryWizardShell / Campaign wizard (z-130); select menus stay at 140. */
        overlayClassName="z-[135]"
        className={cn(
          OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
          "z-[138]"
        )}
      >
        <div className="flex h-full min-h-0 flex-col">
          <header className="flex shrink-0 items-start justify-between gap-3 border-b border-op-card-border px-5 py-4">
            <div className="flex min-w-0 flex-col gap-1">
              <DrawerTitle className="text-lg font-bold text-op-text-primary">
                {CAMPAIGN_OFFER_COPY.createPanelTitle}
              </DrawerTitle>
              <DrawerDescription className="text-xs font-normal text-op-text-muted">
                Define the benefit, validity and rules, then attach this offer
                to the campaign.
              </DrawerDescription>
            </div>
            <DrawerClose asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-8 shrink-0"
                aria-label="Close"
              >
                <XIcon className="size-4" aria-hidden />
              </Button>
            </DrawerClose>
          </header>

          <div className={OPERATOR_RIGHT_DRAWER_BODY_CLASS}>
            <div className="flex flex-col gap-6 px-5 py-5">
              <RecoveryOfferTypeCards
                value={draft.offerType as RecoveryOfferTypeId | null}
                disabled={saving}
                onValueChange={(offerType) => {
                  onPatch({
                    offerType: offerType as CampaignCatalogOfferTypeId,
                  })
                }}
                renderSelectedFields={(offerType) => {
                  if (offerType === "percentage_discount") {
                    return (
                      <div className="flex flex-col gap-2">
                        <label
                          htmlFor="campaign-offer-discount-pct"
                          className={FEEDBACK_FIELD_LABEL_CLASS}
                        >
                          Discount percentage
                        </label>
                        <Input
                          id="campaign-offer-discount-pct"
                          type="number"
                          min={0}
                          step="any"
                          placeholder="0 %"
                          value={draft.discountPercentage}
                          disabled={saving}
                          onChange={(event) => {
                            onPatch({
                              discountPercentage: event.target.value,
                            })
                          }}
                          className={`${FEEDBACK_INPUT_CLASS} h-12`}
                        />
                      </div>
                    )
                  }
                  if (offerType === "fixed_discount") {
                    return (
                      <div className="flex flex-col gap-2">
                        <label
                          htmlFor="campaign-offer-discount-amount"
                          className={FEEDBACK_FIELD_LABEL_CLASS}
                        >
                          Discount amount
                        </label>
                        <Input
                          id="campaign-offer-discount-amount"
                          type="number"
                          min={0}
                          step="any"
                          placeholder="£0.00"
                          value={draft.discountAmount}
                          disabled={saving}
                          onChange={(event) => {
                            onPatch({ discountAmount: event.target.value })
                          }}
                          className={`${FEEDBACK_INPUT_CLASS} h-12`}
                        />
                      </div>
                    )
                  }
                  if (offerType === "free_item") {
                    return (
                      <div className="flex flex-col gap-[18px]">
                        <div className="flex flex-col gap-2">
                          <label
                            htmlFor="campaign-offer-free-item"
                            className={FEEDBACK_FIELD_LABEL_CLASS}
                          >
                            Free item
                          </label>
                          <Input
                            id="campaign-offer-free-item"
                            placeholder="Enter the item the guest can receive…"
                            value={draft.freeItemText}
                            disabled={saving}
                            onChange={(event) => {
                              onPatch({ freeItemText: event.target.value })
                            }}
                            className={`${FEEDBACK_INPUT_CLASS} h-12`}
                          />
                        </div>
                        <RecoveryOfferPurchaseRequirementCards
                          value={
                            draft.purchaseRequirement as RecoveryOfferPurchaseRequirementId | null
                          }
                          disabled={saving}
                          onValueChange={(purchaseRequirement) => {
                            onPatch({ purchaseRequirement })
                          }}
                        />
                        {draft.purchaseRequirement === "with_minimum_spend" ? (
                          <div className="flex flex-col gap-2">
                            <label
                              htmlFor="campaign-offer-min-spend"
                              className={FEEDBACK_FIELD_LABEL_CLASS}
                            >
                              Minimum spend
                            </label>
                            <Input
                              id="campaign-offer-min-spend"
                              type="number"
                              min={0}
                              step="any"
                              placeholder="£0.00"
                              value={draft.minimumSpend}
                              disabled={saving}
                              onChange={(event) => {
                                onPatch({ minimumSpend: event.target.value })
                              }}
                              className={`${FEEDBACK_INPUT_CLASS} h-12`}
                            />
                          </div>
                        ) : null}
                        <div className="flex flex-col gap-2">
                          <label
                            htmlFor="campaign-offer-exclusions"
                            className={FEEDBACK_FIELD_LABEL_CLASS}
                          >
                            Additional exclusions
                          </label>
                          <Textarea
                            id="campaign-offer-exclusions"
                            placeholder="Add any products, dates or conditions that are excluded…"
                            value={draft.additionalExclusions}
                            disabled={saving}
                            onChange={(event) => {
                              onPatch({
                                additionalExclusions: event.target.value,
                              })
                            }}
                            className={`${FEEDBACK_TEXTAREA_CLASS} min-h-[80px]`}
                          />
                        </div>
                      </div>
                    )
                  }
                  return (
                    <div className="flex flex-col gap-2">
                      <label
                        htmlFor="campaign-offer-replacement"
                        className={FEEDBACK_FIELD_LABEL_CLASS}
                      >
                        Replacement item
                      </label>
                      <Input
                        id="campaign-offer-replacement"
                        placeholder="Enter the item the guest can replace…"
                        value={draft.replacementItemText}
                        disabled={saving}
                        onChange={(event) => {
                          onPatch({
                            replacementItemText: event.target.value,
                          })
                        }}
                        className={`${FEEDBACK_INPUT_CLASS} h-12`}
                      />
                    </div>
                  )
                }}
              />

              <div className="flex flex-col gap-2">
                <label
                  htmlFor="campaign-offer-title"
                  className={FEEDBACK_FIELD_LABEL_CLASS}
                >
                  Offer title
                </label>
                <Input
                  id="campaign-offer-title"
                  value={draft.title}
                  maxLength={CAMPAIGN_OFFER_TITLE_MAX}
                  disabled={saving}
                  onChange={(event) => {
                    onPatch({ title: event.target.value })
                  }}
                  className={`${FEEDBACK_INPUT_CLASS} h-12`}
                />
                <p className="text-xs text-op-text-muted">
                  {draft.title.length}/{CAMPAIGN_OFFER_TITLE_MAX}
                </p>
              </div>

              <div className="flex flex-col gap-2">
                <label
                  htmlFor="campaign-offer-description"
                  className={FEEDBACK_FIELD_LABEL_CLASS}
                >
                  Offer description
                </label>
                <Textarea
                  id="campaign-offer-description"
                  value={draft.description}
                  maxLength={CAMPAIGN_OFFER_DESCRIPTION_MAX}
                  disabled={saving}
                  onChange={(event) => {
                    onPatch({ description: event.target.value })
                  }}
                  className={`${FEEDBACK_TEXTAREA_CLASS} min-h-[120px]`}
                />
                <p className="text-xs text-op-text-muted">
                  {draft.description.length}/{CAMPAIGN_OFFER_DESCRIPTION_MAX}
                </p>
              </div>

              <FloatingLabelSelect
                label="Offer validity"
                options={CAMPAIGN_CATALOG_OFFER_VALIDITY_OPTIONS.map(
                  (option) => ({
                    value: option.id,
                    label: option.label,
                  })
                )}
                value={draft.validity}
                onValueChange={(value) => {
                  onPatch({
                    validity: value as CampaignCatalogOfferValidityId,
                  })
                }}
                disabled={saving}
                disableFocusRing
                contentClassName={FEEDBACK_RECOVERY_SELECT_MENU_CLASS}
                itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
              />

              {draft.validity === "choose_expiry_date" ? (
                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="campaign-offer-expiry"
                    className={FEEDBACK_FIELD_LABEL_CLASS}
                  >
                    Expiry date
                  </label>
                  <Input
                    id="campaign-offer-expiry"
                    type="date"
                    value={draft.expiryDate}
                    disabled={saving}
                    onChange={(event) => {
                      onPatch({ expiryDate: event.target.value })
                    }}
                    className={`${FEEDBACK_INPUT_CLASS} h-12`}
                  />
                </div>
              ) : null}

              <div className="flex flex-col gap-2">
                <label
                  htmlFor="campaign-offer-staff"
                  className={FEEDBACK_FIELD_LABEL_CLASS}
                >
                  Staff instructions (optional)
                </label>
                <Textarea
                  id="campaign-offer-staff"
                  value={draft.staffInstructions}
                  disabled={saving}
                  onChange={(event) => {
                    onPatch({ staffInstructions: event.target.value })
                  }}
                  className={`${FEEDBACK_TEXTAREA_CLASS} min-h-[96px]`}
                />
              </div>

              {error != null ? (
                <p className="m-0 text-sm text-destructive" role="alert">
                  {error}
                </p>
              ) : null}
            </div>
          </div>

          <footer className="flex shrink-0 justify-end gap-3 border-t border-op-card-border px-5 py-4">
            <Button
              type="button"
              variant="op-secondary"
              disabled={saving}
              onClick={() => {
                onOpenChange(false)
              }}
            >
              Cancel
            </Button>
            <Button
              type="button"
              variant="op-primary"
              disabled={!canConfirm || saving}
              onClick={onConfirm}
            >
              {saving ? (
                <Loader2Icon className="size-4 animate-spin" aria-hidden />
              ) : null}
              {CAMPAIGN_OFFER_COPY.createPanelConfirm}
            </Button>
          </footer>
        </div>
      </DrawerContent>
    </Drawer>
  )
}
