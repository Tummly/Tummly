import { XIcon } from "lucide-react"

import { CampaignTemplateCard } from "@/components/dashboard/operator/Campaigns/CampaignTemplateCard"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Spinner } from "@/components/ui/spinner"
import {
  CAMPAIGN_TEMPLATE_PICKER_BODY_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_COPY,
  CAMPAIGN_TEMPLATE_PICKER_GRID_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_OVERLAY_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_SEARCH_WRAP_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_SUBTITLE_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_TITLE_CLASS,
} from "@/lib/operatorCampaigns/campaignTemplatePickerPresentation"
import type { CampaignTemplatePickerSnapshot } from "@/lib/operatorCampaigns/createCampaignTemplatePickerModule"
import { CAMPAIGNS_SEARCH_MISS_CLASS } from "@/lib/operatorCampaigns/campaignsPresentation"

type CampaignTemplatePickerDialogProps = {
  snapshot: CampaignTemplatePickerSnapshot
  onOpenChange: (open: boolean) => void
  onRetry: () => void
  onSearchQueryChange: (query: string) => void
  /** Ticket 28 — selecting a template opens the wizard. */
  onUseTemplate?: (templateId: string) => void
  /** S6 — opens Campaign template Preview above the picker. */
  onPreview?: (templateId: string) => void
}

/** Choose a campaign template — Figma 4756:74801. */
export function CampaignTemplatePickerDialog({
  snapshot,
  onOpenChange,
  onRetry,
  onSearchQueryChange,
  onUseTemplate,
  onPreview,
}: CampaignTemplatePickerDialogProps) {
  const copy = CAMPAIGN_TEMPLATE_PICKER_COPY
  const viewModel = snapshot.viewModel
  const isLoaded = snapshot.loadStatus === "loaded" && viewModel != null
  const searchPlaceholder =
    viewModel?.searchPlaceholder ?? copy.searchPlaceholder

  return (
    <Dialog open={snapshot.open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        overlayClassName={CAMPAIGN_TEMPLATE_PICKER_OVERLAY_CLASS}
        className={CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS}
      >
        <DialogHeader className="shrink-0 gap-[30px] p-0">
          <div className="flex items-start gap-[22px]">
            <div className="flex min-w-0 flex-1 flex-col gap-3">
              <DialogTitle className={CAMPAIGN_TEMPLATE_PICKER_TITLE_CLASS}>
                {viewModel?.title ?? copy.title}
              </DialogTitle>
              <DialogDescription
                className={CAMPAIGN_TEMPLATE_PICKER_SUBTITLE_CLASS}
              >
                {viewModel?.subtitle ?? copy.subtitle}
              </DialogDescription>
            </div>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                size="icon"
                className="shrink-0"
                aria-label={copy.closeAriaLabel}
              >
                <XIcon className="size-[18px]" aria-hidden />
              </Button>
            </DialogClose>
          </div>

          {/* Keep search chrome mounted so loading and loaded share header height. */}
          <div className={CAMPAIGN_TEMPLATE_PICKER_SEARCH_WRAP_CLASS}>
            <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-header-search-text" />
            <Input
              value={viewModel?.searchQuery ?? ""}
              onChange={(event) => {
                onSearchQueryChange(event.target.value)
              }}
              disabled={!isLoaded}
              aria-label={searchPlaceholder}
              placeholder={searchPlaceholder}
              className={CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS}
            />
          </div>
        </DialogHeader>

        {snapshot.loadStatus === "loading" || snapshot.loadStatus === "idle" ? (
          <div
            className={`${CAMPAIGN_TEMPLATE_PICKER_BODY_CLASS} items-center justify-center`}
            role="status"
            aria-live="polite"
            aria-label="Loading campaign templates"
          >
            <Spinner />
          </div>
        ) : null}

        {snapshot.loadStatus === "error" ? (
          <div
            className={`${CAMPAIGN_TEMPLATE_PICKER_BODY_CLASS} items-center justify-center gap-3`}
          >
            <p className="m-0 text-sm text-muted-foreground">
              {snapshot.loadError ?? copy.loadError}
            </p>
            <Button
              type="button"
              variant="op-secondary"
              onClick={() => {
                onRetry()
              }}
            >
              {copy.retry}
            </Button>
          </div>
        ) : null}

        {isLoaded ? (
          <div className={`${CAMPAIGN_TEMPLATE_PICKER_BODY_CLASS} gap-[30px]`}>
            {viewModel.showSearchMiss ? (
              <p className={CAMPAIGNS_SEARCH_MISS_CLASS}>
                {copy.searchMiss}
              </p>
            ) : null}
            <div className={CAMPAIGN_TEMPLATE_PICKER_GRID_CLASS}>
              {viewModel.cards.map((card) => (
                <CampaignTemplateCard
                  key={card.id}
                  card={card}
                  onUseTemplate={onUseTemplate}
                  onPreview={onPreview}
                />
              ))}
            </div>
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
