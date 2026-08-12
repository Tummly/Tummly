"use client"

import { format } from "date-fns"
import { CalendarIcon, Loader2Icon, XIcon } from "lucide-react"
import { useEffect, useState } from "react"

import { RecoveryOfferTypeCards } from "@/components/dashboard/operator/Feedback/RecoveryOfferTypeCards"
import { OffersConfirmDialog } from "@/components/dashboard/operator/Offers/OffersConfirmDialog"
import {
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_INPUT_CLASS,
  FEEDBACK_RECOVERY_SELECT_MENU_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import {
  Drawer,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { Input } from "@/components/ui/input"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { Textarea } from "@/components/ui/textarea"
import {
  CREATE_EDIT_OFFER_DISCARD_CONTENT_CLASS,
  CREATE_EDIT_OFFER_DISCARD_OVERLAY_CLASS,
  CREATE_EDIT_OFFER_DRAWER_BODY_CLASS,
  CREATE_EDIT_OFFER_DRAWER_COPY,
  CREATE_EDIT_OFFER_DRAWER_DIVIDER_CLASS,
  CREATE_EDIT_OFFER_DRAWER_FOOTER_ACTIONS_CLASS,
  CREATE_EDIT_OFFER_DRAWER_SHELL_CLASS,
  createEditOfferDrawerConfirmLabel,
  createEditOfferDrawerShowsTypePicker,
  createEditOfferDrawerTitle,
  type CreateEditOfferDrawerMode,
} from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import {
  CAMPAIGN_CATALOG_OFFER_PURCHASE_REQUIREMENT_OPTIONS,
  CAMPAIGN_CATALOG_OFFER_TYPE_OPTIONS,
  CAMPAIGN_CATALOG_OFFER_VALIDITY_OPTIONS,
  CAMPAIGN_OFFER_DESCRIPTION_MAX,
  CAMPAIGN_OFFER_TITLE_MAX,
  type CampaignCatalogOfferDetailsDraft,
  type CampaignCatalogOfferPurchaseRequirementId,
  type CampaignCatalogOfferTypeId,
  type CampaignCatalogOfferValidityId,
} from "@/lib/operatorOffers/offerCatalogPresentation"
import { OPERATOR_RIGHT_DRAWER_CONTENT_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import {
  parseLocalDateKey,
  toLocalDateKey,
} from "@/lib/operatorHome/homePerformanceDateRange"
import type { RecoveryOfferTypeId } from "@/lib/operatorFeedback/recoveryOfferPresentation"
import { cn } from "@/lib/utils"

const EXPIRY_DATE_TRIGGER_CLASS = cn(
  FEEDBACK_INPUT_CLASS,
  "h-12 w-full justify-start gap-3 px-[15px] py-[15px] text-left font-normal shadow-none hover:bg-transparent"
)

export type CreateEditOfferDrawerProps = {
  open: boolean
  mode: CreateEditOfferDrawerMode
  locationSubtitle: string
  draft: CampaignCatalogOfferDetailsDraft
  canConfirm: boolean
  saveGated: boolean
  status: "idle" | "saving" | "error"
  error: string | null
  onOpenChange: (open: boolean) => void
  onPatch: (patch: Partial<CampaignCatalogOfferDetailsDraft>) => void
  onConfirm: () => void
}

function OfferTypeReadOnly({
  offerType,
}: {
  offerType: CampaignCatalogOfferTypeId
}) {
  const option = CAMPAIGN_CATALOG_OFFER_TYPE_OPTIONS.find(
    (entry) => entry.id === offerType
  )
  if (option == null) {
    return null
  }

  return (
    <div className="flex gap-2.5 rounded-[4px] border border-op-text-muted bg-transparent px-[18px] py-4">
      <div className="flex min-w-0 flex-col gap-1">
        <p className="m-0 text-sm font-medium text-op-text-primary">
          {option.label}
        </p>
        <p className="m-0 text-xs font-medium text-op-text-muted">
          {option.description}
        </p>
      </div>
    </div>
  )
}

function RedemptionExplain() {
  const copy = CREATE_EDIT_OFFER_DRAWER_COPY
  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-2">
        <p className={FEEDBACK_FIELD_LABEL_CLASS}>{copy.redemptionLabel}</p>
        <div
          className={cn(
            FEEDBACK_INPUT_CLASS,
            "flex h-12 items-center bg-[rgba(54,54,56,0.1)] dark:bg-[rgba(54,54,56,0.1)]"
          )}
          aria-readonly
        >
          <span className="text-sm text-op-input-placeholder">
            {copy.redemptionValue}
          </span>
        </div>
      </div>
      <p className="m-0 text-xs font-medium leading-4 text-op-text-muted">
        {copy.redemptionHelper}
      </p>
    </div>
  )
}

function RecoveryUseOnlyCallout() {
  const copy = CREATE_EDIT_OFFER_DRAWER_COPY
  return (
    <div
      className="flex flex-col gap-2.5 rounded-[4px] bg-op-card-border p-[18px] dark:bg-[#262626]"
      role="note"
    >
      <p className="m-0 text-sm font-semibold text-op-text-primary">
        {copy.recoveryCalloutTitle}
      </p>
      <p className="m-0 text-xs font-medium text-op-text-muted">
        {copy.recoveryCalloutBody}
      </p>
    </div>
  )
}

function TypeSpecificFields({
  draft,
  saving,
  onPatch,
  idPrefix,
}: {
  draft: CampaignCatalogOfferDetailsDraft
  saving: boolean
  onPatch: (patch: Partial<CampaignCatalogOfferDetailsDraft>) => void
  idPrefix: string
}) {
  const offerType = draft.offerType
  if (offerType == null) {
    return null
  }

  if (offerType === "percentage_discount") {
    return (
      <div className="flex flex-col gap-2">
        <label
          htmlFor={`${idPrefix}-discount-pct`}
          className={FEEDBACK_FIELD_LABEL_CLASS}
        >
          Discount percentage
        </label>
        <Input
          id={`${idPrefix}-discount-pct`}
          type="number"
          min={0}
          step="any"
          placeholder="0 %"
          value={draft.discountPercentage}
          disabled={saving}
          onChange={(event) => {
            onPatch({ discountPercentage: event.target.value })
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
          htmlFor={`${idPrefix}-discount-amount`}
          className={FEEDBACK_FIELD_LABEL_CLASS}
        >
          Discount amount
        </label>
        <Input
          id={`${idPrefix}-discount-amount`}
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
            htmlFor={`${idPrefix}-free-item`}
            className={FEEDBACK_FIELD_LABEL_CLASS}
          >
            Free item
          </label>
          <Input
            id={`${idPrefix}-free-item`}
            placeholder="Enter the item the guest can receive…"
            value={draft.freeItemText}
            disabled={saving}
            onChange={(event) => {
              onPatch({ freeItemText: event.target.value })
            }}
            className={`${FEEDBACK_INPUT_CLASS} h-12`}
          />
        </div>
        <FloatingLabelSelect
          label="Purchase requirement"
          options={CAMPAIGN_CATALOG_OFFER_PURCHASE_REQUIREMENT_OPTIONS.map(
            (option) => ({
              value: option.id,
              label: option.label,
            })
          )}
          value={draft.purchaseRequirement ?? ""}
          onValueChange={(value) => {
            onPatch({
              purchaseRequirement:
                value as CampaignCatalogOfferPurchaseRequirementId,
            })
          }}
          disabled={saving}
          disableFocusRing
          contentClassName={FEEDBACK_RECOVERY_SELECT_MENU_CLASS}
          itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
        />
        {draft.purchaseRequirement === "with_minimum_spend" ? (
          <>
            <div className="flex flex-col gap-2">
              <label
                htmlFor={`${idPrefix}-min-spend`}
                className={FEEDBACK_FIELD_LABEL_CLASS}
              >
                Minimum spend
              </label>
              <Input
                id={`${idPrefix}-min-spend`}
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
            <div className="flex flex-col gap-2">
              <label
                htmlFor={`${idPrefix}-exclusions`}
                className={FEEDBACK_FIELD_LABEL_CLASS}
              >
                Additional exclusions
              </label>
              <Textarea
                id={`${idPrefix}-exclusions`}
                placeholder="Add any products, dates or conditions that are excluded…"
                value={draft.additionalExclusions}
                disabled={saving}
                onChange={(event) => {
                  onPatch({ additionalExclusions: event.target.value })
                }}
                className={`${FEEDBACK_TEXTAREA_CLASS} min-h-[80px]`}
              />
            </div>
          </>
        ) : null}
      </div>
    )
  }

  const replacementOption = CAMPAIGN_CATALOG_OFFER_TYPE_OPTIONS.find(
    (entry) => entry.id === "replacement_item"
  )

  return (
    <div className="flex flex-col gap-[18px]">
      <div className="flex flex-col gap-3">
        <div className="flex flex-col gap-2">
          <label
            htmlFor={`${idPrefix}-replacement`}
            className={FEEDBACK_FIELD_LABEL_CLASS}
          >
            Replacement item
          </label>
          <Input
            id={`${idPrefix}-replacement`}
            placeholder={CREATE_EDIT_OFFER_DRAWER_COPY.replacementPlaceholder}
            value={draft.replacementItemText}
            disabled={saving}
            onChange={(event) => {
              onPatch({ replacementItemText: event.target.value })
            }}
            className={`${FEEDBACK_INPUT_CLASS} h-12`}
          />
        </div>
        {replacementOption != null ? (
          <p className="m-0 text-xs font-medium text-op-text-muted">
            {replacementOption.description}
          </p>
        ) : null}
      </div>
      <RecoveryUseOnlyCallout />
    </div>
  )
}

function OfferExpiryDateField({
  id,
  value,
  disabled,
  onChange,
}: {
  id: string
  value: string
  disabled: boolean
  onChange: (next: string) => void
}) {
  const [open, setOpen] = useState(false)
  const selectedDate =
    value.trim().length > 0 ? parseLocalDateKey(value) : undefined
  const hasDate =
    selectedDate != null && !Number.isNaN(selectedDate.getTime())

  return (
    <div className="flex flex-col gap-2">
      <label htmlFor={id} className={FEEDBACK_FIELD_LABEL_CLASS}>
        Expiry date
      </label>
      <Popover open={open} onOpenChange={setOpen} modal>
        <PopoverTrigger asChild>
          <Button
            id={id}
            type="button"
            variant="op-ghost"
            disabled={disabled}
            className={cn(
              EXPIRY_DATE_TRIGGER_CLASS,
              !hasDate && "text-op-input-placeholder"
            )}
          >
            <CalendarIcon
              className="size-4 shrink-0 text-op-text-primary"
              aria-hidden
            />
            <span className="truncate text-sm leading-5">
              {hasDate
                ? format(selectedDate, "d MMM yyyy")
                : CREATE_EDIT_OFFER_DRAWER_COPY.expiryDatePlaceholder}
            </span>
          </Button>
        </PopoverTrigger>
        <PopoverContent
          align="start"
          className={cn("w-auto p-0", FEEDBACK_RECOVERY_SELECT_MENU_CLASS)}
        >
          <Calendar
            mode="single"
            selected={hasDate ? selectedDate : undefined}
            defaultMonth={hasDate ? selectedDate : undefined}
            onSelect={(date) => {
              if (date == null) {
                return
              }
              onChange(toLocalDateKey(date))
              setOpen(false)
            }}
          />
        </PopoverContent>
      </Popover>
      <p className="m-0 text-xs font-medium text-op-text-muted">
        The offer will expire at the end of this date in the restaurant’s
        timezone.
      </p>
    </div>
  )
}

/**
 * Shared Offers Create/Edit drawer — Figma chrome; Campaign + Offers callers.
 */
export function CreateEditOfferDrawer({
  open,
  mode,
  locationSubtitle,
  draft,
  canConfirm,
  saveGated,
  status,
  error,
  onOpenChange,
  onPatch,
  onConfirm,
}: CreateEditOfferDrawerProps) {
  const saving = status === "saving"
  const copy = CREATE_EDIT_OFFER_DRAWER_COPY
  const idPrefix = mode === "edit" ? "edit-offer" : "create-offer"
  const [discardOpen, setDiscardOpen] = useState(false)
  const showDetails = draft.offerType != null

  useEffect(() => {
    if (open) {
      setDiscardOpen(false)
    }
  }, [open])

  function requestClose() {
    if (saving) {
      return
    }
    setDiscardOpen(true)
  }

  function confirmDiscard() {
    setDiscardOpen(false)
    onOpenChange(false)
  }

  return (
    <>
      <Drawer
        direction="right"
        open={open}
        onOpenChange={(nextOpen) => {
          if (!nextOpen) {
            requestClose()
            return
          }
          onOpenChange(true)
        }}
        shouldScaleBackground={false}
      >
        <DrawerContent
          /* Above RecoveryWizardShell / Campaign wizard (z-130); select menus stay at 140. */
          overlayClassName="z-[135]"
          className={cn(OPERATOR_RIGHT_DRAWER_CONTENT_CLASS, "z-[138]")}
        >
          <div className={CREATE_EDIT_OFFER_DRAWER_SHELL_CLASS}>
            <header className="flex shrink-0 items-start gap-[22px]">
              <div className="flex min-w-0 flex-1 flex-col gap-3">
                <DrawerTitle className="text-2xl font-bold leading-normal tracking-normal text-op-text-primary">
                  {createEditOfferDrawerTitle(mode)}
                </DrawerTitle>
                <DrawerDescription className="text-sm font-medium leading-normal text-op-text-muted">
                  {locationSubtitle}
                </DrawerDescription>
              </div>
              <Button
                type="button"
                variant="op-collapse"
                size="icon"
                className="shrink-0"
                aria-label="Close"
                disabled={saving}
                onClick={requestClose}
              >
                <XIcon aria-hidden />
              </Button>
            </header>

            <div className={CREATE_EDIT_OFFER_DRAWER_DIVIDER_CLASS} aria-hidden />

            <div className={CREATE_EDIT_OFFER_DRAWER_BODY_CLASS}>
              <div
                className={cn(
                  "flex flex-col",
                  showDetails ? "gap-7" : "gap-[18px]"
                )}
              >
                {createEditOfferDrawerShowsTypePicker(mode) ? (
                  <RecoveryOfferTypeCards
                    value={draft.offerType as RecoveryOfferTypeId | null}
                    disabled={saving}
                    onValueChange={(offerType) => {
                      onPatch({
                        offerType: offerType as CampaignCatalogOfferTypeId,
                      })
                    }}
                    renderSelectedFields={() => (
                      <TypeSpecificFields
                        draft={draft}
                        saving={saving}
                        onPatch={onPatch}
                        idPrefix={idPrefix}
                      />
                    )}
                  />
                ) : (
                  <div className="flex flex-col gap-[18px]">
                    {draft.offerType != null ? (
                      <>
                        <OfferTypeReadOnly offerType={draft.offerType} />
                        <TypeSpecificFields
                          draft={draft}
                          saving={saving}
                          onPatch={onPatch}
                          idPrefix={idPrefix}
                        />
                      </>
                    ) : (
                      <div
                        className="flex items-center justify-center py-8"
                        role="status"
                        aria-live="polite"
                        aria-label="Loading offer"
                      >
                        <Loader2Icon
                          className="size-5 animate-spin text-op-text-muted"
                          aria-hidden
                        />
                      </div>
                    )}
                  </div>
                )}

                {showDetails ? (
                  <>
                    <div
                      className={CREATE_EDIT_OFFER_DRAWER_DIVIDER_CLASS}
                      aria-hidden
                    />

                    <div className="flex flex-col gap-[18px]">
                      <div className="flex flex-col gap-2">
                        <label
                          htmlFor={`${idPrefix}-title`}
                          className={FEEDBACK_FIELD_LABEL_CLASS}
                        >
                          Offer title
                        </label>
                        <Input
                          id={`${idPrefix}-title`}
                          value={draft.title}
                          maxLength={CAMPAIGN_OFFER_TITLE_MAX}
                          disabled={saving}
                          onChange={(event) => {
                            onPatch({ title: event.target.value })
                          }}
                          className={`${FEEDBACK_INPUT_CLASS} h-12`}
                        />
                      </div>

                      <div className="flex flex-col gap-2">
                        <label
                          htmlFor={`${idPrefix}-description`}
                          className={FEEDBACK_FIELD_LABEL_CLASS}
                        >
                          Offer description
                        </label>
                        <Textarea
                          id={`${idPrefix}-description`}
                          value={draft.description}
                          maxLength={CAMPAIGN_OFFER_DESCRIPTION_MAX}
                          disabled={saving}
                          placeholder={copy.descriptionPlaceholder}
                          onChange={(event) => {
                            onPatch({ description: event.target.value })
                          }}
                          className={`${FEEDBACK_TEXTAREA_CLASS} min-h-[120px]`}
                        />
                      </div>
                    </div>

                    <div
                      className={CREATE_EDIT_OFFER_DRAWER_DIVIDER_CLASS}
                      aria-hidden
                    />

                    <div className="flex flex-col gap-[18px]">
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
                        <OfferExpiryDateField
                          id={`${idPrefix}-expiry`}
                          value={draft.expiryDate}
                          disabled={saving}
                          onChange={(expiryDate) => {
                            onPatch({ expiryDate })
                          }}
                        />
                      ) : null}

                      <RedemptionExplain />

                      <div className="flex flex-col gap-2">
                        <label
                          htmlFor={`${idPrefix}-staff`}
                          className={FEEDBACK_FIELD_LABEL_CLASS}
                        >
                          {copy.staffInstructionsLabel}
                        </label>
                        <Textarea
                          id={`${idPrefix}-staff`}
                          value={draft.staffInstructions}
                          disabled={saving}
                          onChange={(event) => {
                            onPatch({ staffInstructions: event.target.value })
                          }}
                          className={`${FEEDBACK_TEXTAREA_CLASS} min-h-[96px]`}
                        />
                      </div>
                    </div>
                  </>
                ) : null}

                {error != null ? (
                  <p className="m-0 text-sm text-destructive" role="alert">
                    {error}
                  </p>
                ) : null}
              </div>
            </div>

            <footer className="flex shrink-0 flex-col gap-2 pt-1">
              {saveGated ? (
                <p className="m-0 text-xs text-op-text-muted" role="status">
                  {copy.editSaveGatedHelper}
                </p>
              ) : null}
              <div className={CREATE_EDIT_OFFER_DRAWER_FOOTER_ACTIONS_CLASS}>
                <Button
                  type="button"
                  variant="op-primary"
                  disabled={!canConfirm || saving || saveGated}
                  onClick={onConfirm}
                >
                  {saving ? (
                    <Loader2Icon className="size-4 animate-spin" aria-hidden />
                  ) : null}
                  {createEditOfferDrawerConfirmLabel(mode)}
                </Button>
                <Button
                  type="button"
                  variant="op-tertiary"
                  disabled={saving}
                  onClick={requestClose}
                >
                  {copy.cancel}
                </Button>
              </div>
            </footer>
          </div>
        </DrawerContent>
      </Drawer>

      <OffersConfirmDialog
        open={discardOpen}
        title={copy.discardTitle}
        description={copy.discardDescription}
        confirmLabel={copy.discardConfirm}
        cancelLabel={copy.discardKeepEditing}
        overlayClassName={CREATE_EDIT_OFFER_DISCARD_OVERLAY_CLASS}
        className={CREATE_EDIT_OFFER_DISCARD_CONTENT_CLASS}
        onOpenChange={(nextOpen) => {
          if (!nextOpen) {
            setDiscardOpen(false)
          }
        }}
        onConfirm={confirmDiscard}
      />
    </>
  )
}
