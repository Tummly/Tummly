import { Button } from "@/components/ui/button"
import {
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  formatNeedsAttentionSubtitle,
  LOCATIONS_CARD_CLASS,
  LOCATIONS_NEEDS_ATTENTION_ROW_CLASS,
  LOCATIONS_NEEDS_ATTENTION_ROW_COPY_CLASS,
  LOCATIONS_PAGE_COPY,
  type LocationsSetupAttentionItem,
  type LocationsSetupAttentionItemId,
} from "@/lib/operatorLocations/locationsPresentation"
import { cn } from "@/lib/utils"

type LocationsSetupReadinessSectionProps = {
  items: readonly LocationsSetupAttentionItem[]
  onReviewLocation: (itemId: LocationsSetupAttentionItemId) => void
}

/** Setup & readiness — Needs attention card (Figma 5748:103603). */
export function LocationsSetupReadinessSection({
  items,
  onReviewLocation,
}: LocationsSetupReadinessSectionProps) {
  const copy = LOCATIONS_PAGE_COPY

  return (
    <section
      className={cn(LOCATIONS_CARD_CLASS, "gap-10")}
      aria-label={copy.needsAttentionTitle}
    >
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.needsAttentionTitle}</h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
          {formatNeedsAttentionSubtitle(items.length)}
        </p>
      </header>

      {items.length === 0 ? (
        <div className="flex flex-col gap-2">
          <p className="m-0 text-sm font-medium text-op-text-primary">
            {copy.needsAttentionEmptyTitle}
          </p>
          <p className="m-0 text-sm font-medium text-op-card-subtitle-color">
            {copy.needsAttentionEmptyBody}
          </p>
        </div>
      ) : (
        <div className="flex w-full flex-col gap-3">
          {items.map((item) => (
            <div key={item.id} className={LOCATIONS_NEEDS_ATTENTION_ROW_CLASS}>
              <p className={LOCATIONS_NEEDS_ATTENTION_ROW_COPY_CLASS}>
                {item.message}
              </p>
              <Button
                type="button"
                variant="op-tertiary"
                className="shrink-0 rounded-[2px]"
                onClick={() => {
                  onReviewLocation(item.id)
                }}
              >
                {copy.reviewLocation}
              </Button>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}
