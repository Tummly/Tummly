import { XIcon } from "lucide-react"

import { OfferTemplateCard } from "@/components/dashboard/operator/Offers/OfferTemplateCard"
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
import type { OfferTemplatePickerSnapshot } from "@/lib/operatorOffers/createOfferTemplatePickerModule"
import {
  OFFER_TEMPLATE_PICKER_BODY_CLASS,
  OFFER_TEMPLATE_PICKER_CONTENT_CLASS,
  OFFER_TEMPLATE_PICKER_COPY,
  OFFER_TEMPLATE_PICKER_GRID_CLASS,
  OFFER_TEMPLATE_PICKER_OVERLAY_CLASS,
  OFFER_TEMPLATE_PICKER_SEARCH_FIELD_CLASS,
  OFFER_TEMPLATE_PICKER_SEARCH_WRAP_CLASS,
  OFFER_TEMPLATE_PICKER_SUBTITLE_CLASS,
  OFFER_TEMPLATE_PICKER_TITLE_CLASS,
} from "@/lib/operatorOffers/offerTemplatePickerPresentation"
import { OFFERS_SEARCH_MISS_CLASS } from "@/lib/operatorOffers/offersPresentation"

type OfferTemplatePickerDialogProps = {
  snapshot: OfferTemplatePickerSnapshot
  onOpenChange: (open: boolean) => void
  onRetry: () => void
  onSearchQueryChange: (query: string) => void
  onUseTemplate?: (templateId: string) => void
}

/** Choose an offer template — Figma 4783:30859. */
export function OfferTemplatePickerDialog({
  snapshot,
  onOpenChange,
  onRetry,
  onSearchQueryChange,
  onUseTemplate,
}: OfferTemplatePickerDialogProps) {
  const copy = OFFER_TEMPLATE_PICKER_COPY
  const viewModel = snapshot.viewModel
  const isLoaded = snapshot.loadStatus === "loaded" && viewModel != null
  const searchPlaceholder =
    viewModel?.searchPlaceholder ?? copy.searchPlaceholder

  return (
    <Dialog open={snapshot.open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        overlayClassName={OFFER_TEMPLATE_PICKER_OVERLAY_CLASS}
        className={OFFER_TEMPLATE_PICKER_CONTENT_CLASS}
      >
        <DialogHeader className="shrink-0 gap-[30px] p-0">
          <div className="flex items-start gap-[22px]">
            <div className="flex min-w-0 flex-1 flex-col gap-3">
              <DialogTitle className={OFFER_TEMPLATE_PICKER_TITLE_CLASS}>
                {viewModel?.title ?? copy.title}
              </DialogTitle>
              <DialogDescription
                className={OFFER_TEMPLATE_PICKER_SUBTITLE_CLASS}
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

          <div className={OFFER_TEMPLATE_PICKER_SEARCH_WRAP_CLASS}>
            <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-header-search-text" />
            <Input
              value={viewModel?.searchQuery ?? ""}
              onChange={(event) => {
                onSearchQueryChange(event.target.value)
              }}
              disabled={!isLoaded}
              aria-label={searchPlaceholder}
              placeholder={searchPlaceholder}
              className={OFFER_TEMPLATE_PICKER_SEARCH_FIELD_CLASS}
            />
          </div>
        </DialogHeader>

        {snapshot.loadStatus === "loading" || snapshot.loadStatus === "idle" ? (
          <div
            className={`${OFFER_TEMPLATE_PICKER_BODY_CLASS} items-center justify-center`}
            role="status"
            aria-live="polite"
            aria-label="Loading offer templates"
          >
            <Spinner />
          </div>
        ) : null}

        {snapshot.loadStatus === "error" ? (
          <div
            className={`${OFFER_TEMPLATE_PICKER_BODY_CLASS} items-center justify-center gap-3`}
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
          <div className={`${OFFER_TEMPLATE_PICKER_BODY_CLASS} gap-[30px]`}>
            {viewModel.showSearchMiss ? (
              <p className={OFFERS_SEARCH_MISS_CLASS}>{copy.searchMiss}</p>
            ) : null}
            <div className={OFFER_TEMPLATE_PICKER_GRID_CLASS}>
              {viewModel.cards.map((card) => (
                <OfferTemplateCard
                  key={card.id}
                  card={card}
                  onUseTemplate={onUseTemplate}
                />
              ))}
            </div>
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
