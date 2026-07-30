import type { ReactNode } from "react"
import { MoreVerticalIcon, PackageIcon, XIcon } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Textarea } from "@/components/ui/textarea"
import {
  CAPTURE_PLACEMENT_DETAIL_SECTION_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS,
  OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { PlacementDetailDrawerView } from "@/lib/operatorCapture/buildPlacementDetailDrawer"
import type { PlacementDetailDrawerSnapshot } from "@/lib/operatorCapture/createOperatorCapturePageModule"
import {
  OPERATOR_DRAWER_ACTION_ROW_CLASS,
  OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

type CapturePlacementDetailDrawerProps = {
  snapshot: PlacementDetailDrawerSnapshot
  onOpenChange: (open: boolean) => void
  onPreview: () => void
  onCopyLink: () => void
  onPause: () => void
  onActivate: () => void
  onRotate: () => void
  onArchive: () => void
  onDescriptionDraftChange: (value: string) => void
  onSaveDescription: () => void
}

function Section({
  title,
  children,
  className,
}: {
  title: string
  children: ReactNode
  className?: string
}) {
  return (
    <section className={cn(CAPTURE_PLACEMENT_DETAIL_SECTION_CLASS, className)}>
      <h3 className="text-lg font-bold text-foreground">{title}</h3>
      {children}
    </section>
  )
}

function MetricRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <p className="text-base font-medium text-foreground">{label}</p>
      <p className="text-sm font-medium text-[var(--op-color-gray-550)]">{value}</p>
    </div>
  )
}

function DetailField({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <p className="text-base font-medium text-foreground">{label}</p>
      {children}
    </div>
  )
}

function LoadedBody({
  details,
  onPreview,
  onCopyLink,
  onPause,
  onActivate,
  onRotate,
  onArchive,
  onDescriptionDraftChange,
  onSaveDescription,
}: {
  details: PlacementDetailDrawerView
  onPreview: () => void
  onCopyLink: () => void
  onPause: () => void
  onActivate: () => void
  onRotate: () => void
  onArchive: () => void
  onDescriptionDraftChange: (value: string) => void
  onSaveDescription: () => void
}) {
  const copy = OPERATOR_CAPTURE_PLACEMENT_DETAIL_COPY
  const trimmedDescription = details.descriptionDraft.trim()
  const canSaveDescription =
    trimmedDescription.length > 0
    && trimmedDescription.length <= details.descriptionMaxLength

  return (
    <>
      <div className="flex shrink-0 flex-col gap-[22px] px-[22px] pb-[22px] pt-8">
        <div className="flex items-start justify-between gap-4">
          <div className="flex min-w-0 flex-col gap-2">
            <DrawerTitle className="text-2xl font-bold text-foreground">
              {details.title}
            </DrawerTitle>
            <DrawerDescription className="sr-only">
              {details.detailsSectionTitle} for {details.title}
            </DrawerDescription>
            <div className="flex items-center gap-2">
              <Badge variant="soft">{details.status}</Badge>
              <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
                {details.locationName}
              </p>
            </div>
          </div>
          <DrawerClose asChild>
            <Button
              type="button"
              variant="op-ghost"
              size="icon"
              aria-label={copy.closeLabel}
              className="size-[42px] shrink-0 rounded-[2px] bg-[var(--op-color-gray-950)] text-white hover:bg-[var(--op-color-gray-950)]/90 hover:text-white"
            >
              <XIcon className="size-[18px]" aria-hidden />
            </Button>
          </DrawerClose>
        </div>

        <div className="flex items-center justify-between gap-3">
          <div className={OPERATOR_DRAWER_ACTION_ROW_CLASS}>
            <Button
              type="button"
              variant="op-primary"
              disabled={!details.editGuestFormEnabled}
              aria-disabled={!details.editGuestFormEnabled}
              className={cn(
                OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
                "w-fit rounded-[2px]"
              )}
            >
              {copy.editGuestFormCta}
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              disabled={!details.previewGuestExperienceEnabled}
              aria-disabled={!details.previewGuestExperienceEnabled}
              className={cn(
                OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
                "w-fit rounded-[2px]"
              )}
              onClick={onPreview}
            >
              {copy.previewGuestExperienceCta}
            </Button>
          </div>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                type="button"
                variant="op-ghost"
                size="icon"
                aria-label={copy.moreActionsLabel}
                className="size-8 shrink-0"
              >
                <MoreVerticalIcon className="size-4" aria-hidden />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="end"
              className={CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS}
            >
              <DropdownMenuItem
                className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
                onClick={onCopyLink}
              >
                {copy.copyGuestLink}
              </DropdownMenuItem>
              {details.canPauseOrActivate && details.pauseActivateLabel != null ? (
                <DropdownMenuItem
                  className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
                  onClick={() => {
                    if (details.status === "Active") {
                      onPause()
                      return
                    }
                    onActivate()
                  }}
                >
                  {details.pauseActivateLabel}
                </DropdownMenuItem>
              ) : null}
              {details.canRotate ? (
                <DropdownMenuItem
                  className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
                  onClick={onRotate}
                >
                  {copy.rotateQrCode}
                </DropdownMenuItem>
              ) : null}
              {details.canArchive ? (
                <DropdownMenuItem
                  className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
                  onClick={onArchive}
                >
                  {copy.archivePlacement}
                </DropdownMenuItem>
              ) : null}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      <Section title={copy.performanceTitle}>
        <MetricRow
          label={copy.guestFormOpensLabel}
          value={details.guestFormOpensText}
        />
        <MetricRow
          label={copy.feedbackSubmittedLabel}
          value={details.feedbackSubmittedText}
        />
        <MetricRow
          label={copy.marketingOptInsLabel}
          value={details.marketingOptInsText}
        />
        <MetricRow
          label={copy.offerClaimsLabel}
          value={details.offerClaimsText}
        />
        <MetricRow
          label={copy.submissionRateLabel}
          value={details.submissionRateText}
        />
        <MetricRow label={copy.lastScanLabel} value={details.lastScanText} />
      </Section>

      <Section title={details.detailsSectionTitle}>
        <DetailField label={`${details.typeFieldLabel}:`}>
          <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
            {details.typeValue}
          </p>
        </DetailField>
        {details.channelLabel != null ? (
          <DetailField label={copy.whereUsedLabel}>
            <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
              {details.channelLabel}
            </p>
          </DetailField>
        ) : null}
        <DetailField label={copy.statusLabel}>
          <Badge variant="soft">{details.status}</Badge>
        </DetailField>
        <DetailField label={copy.connectedGuestFormLabel}>
          <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
            {details.connectedGuestForm}
          </p>
        </DetailField>
        <DetailField label={copy.createdLabel}>
          <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
            {details.createdDisplay}
          </p>
        </DetailField>
        <DetailField label={copy.lastUpdatedLabel}>
          <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
            {details.lastUpdatedDisplay}
          </p>
        </DetailField>
        <DetailField label={copy.connectedOfferLabel}>
          <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
            {details.connectedOfferText}
          </p>
        </DetailField>
      </Section>

      <Section title={details.assetsSectionTitle}>
        <div className="flex flex-wrap items-center gap-3">
          {details.showOrderPrintMaterials ? (
            <Button
              type="button"
              variant="op-primary"
              disabled={!details.orderPrintMaterialsEnabled}
              aria-disabled={!details.orderPrintMaterialsEnabled}
              className={cn(
                OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
                "w-fit gap-2 rounded-[2px]"
              )}
            >
              <PackageIcon className="size-4" aria-hidden />
              {copy.orderPrintMaterials}
            </Button>
          ) : null}
          <Button
            type="button"
            variant="op-tertiary"
            className={cn(
              OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
              "w-fit rounded-[2px]"
            )}
            onClick={onCopyLink}
          >
            {copy.copyGuestLink}
          </Button>
        </div>
      </Section>

      <Section title={copy.internalDescriptionTitle} className="gap-[22px]">
        <div className="flex flex-col gap-3">
          <Textarea
            value={details.descriptionDraft}
            onChange={(event) => {
              onDescriptionDraftChange(event.target.value)
            }}
            maxLength={details.descriptionMaxLength}
            rows={3}
            placeholder={details.descriptionPlaceholder}
            className="min-h-0 resize-none rounded-[4px] border-op-input-border px-[13px] py-[15px] text-sm placeholder:text-guest-feedback-placeholder dark:bg-transparent"
          />
          <Button
            type="button"
            variant="op-secondary"
            disabled={!canSaveDescription}
            aria-disabled={!canSaveDescription}
            className={cn(
              OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
              "w-fit rounded-[2px]"
            )}
            onClick={onSaveDescription}
          >
            {copy.addNoteCta}
          </Button>
        </div>
      </Section>
    </>
  )
}

/** Shared Placement / Link Detail drawer — Figma `3889:28072`. */
export function CapturePlacementDetailDrawer({
  snapshot,
  onOpenChange,
  onPreview,
  onCopyLink,
  onPause,
  onActivate,
  onRotate,
  onArchive,
  onDescriptionDraftChange,
  onSaveDescription,
}: CapturePlacementDetailDrawerProps) {
  return (
    <Drawer
      open={snapshot.isOpen}
      onOpenChange={onOpenChange}
      direction="right"
    >
      <DrawerContent
        className={cn(
          OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
          "dark:bg-[var(--op-color-gray-995)]"
        )}
      >
        <div className="flex min-h-0 flex-1 flex-col">
          {snapshot.details != null ? (
            <div className={OPERATOR_RIGHT_DRAWER_BODY_CLASS}>
              <LoadedBody
                details={snapshot.details}
                onPreview={onPreview}
                onCopyLink={onCopyLink}
                onPause={onPause}
                onActivate={onActivate}
                onRotate={onRotate}
                onArchive={onArchive}
                onDescriptionDraftChange={onDescriptionDraftChange}
                onSaveDescription={onSaveDescription}
              />
            </div>
          ) : null}
        </div>
      </DrawerContent>
    </Drawer>
  )
}
