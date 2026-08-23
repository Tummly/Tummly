import {
  NeedsAttentionEmpty,
  NeedsAttentionRow,
  NeedsAttentionRowList,
  NeedsAttentionSection,
} from "@/components/dashboard/operator/NeedsAttentionSection"
import { Button } from "@/components/ui/button"
import type { OperatorOffersNeedsAttentionView } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import type { OffersNeedsAttentionOverviewRow } from "@/lib/operatorOffers/buildOffersNeedsAttentionOverview"
import { GUESTS_PAGE_SECONDARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import { NEEDS_ATTENTION_VIEW_ALL_ROW_CLASS } from "@/lib/operatorHome/operatorHomeSectionPresentation"

type OffersNeedsAttentionSectionProps = {
  needsAttention: OperatorOffersNeedsAttentionView
  onRowCta: (row: OffersNeedsAttentionOverviewRow) => void
  onViewAll: () => void
}

/** Offers Needs attention — shared accordion and Figma rows. */
export function OffersNeedsAttentionSection({
  needsAttention,
  onRowCta,
  onViewAll,
}: OffersNeedsAttentionSectionProps) {
  return (
    <NeedsAttentionSection
      title={needsAttention.title}
      subtitle={needsAttention.subtitle}
    >
      {needsAttention.isEmpty ? (
        <NeedsAttentionEmpty copy={needsAttention.emptyCopy} />
      ) : (
        <NeedsAttentionRowList>
          {needsAttention.rows.map((row) => (
            <NeedsAttentionRow
              key={row.id}
              title={row.title}
              body={row.body}
              metaLine={row.metaLine}
              tone={row.kind}
              icon="tag"
              actions={[
                {
                  key: row.ctaKind,
                  label: row.ctaLabel,
                  onClick: () => {
                    onRowCta(row)
                  },
                },
              ]}
            />
          ))}
          {needsAttention.showViewAll ? (
            <div className={NEEDS_ATTENTION_VIEW_ALL_ROW_CLASS}>
              <Button
                type="button"
                variant="op-tertiary"
                className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                onClick={onViewAll}
              >
                {needsAttention.viewAllLabel}
              </Button>
            </div>
          ) : null}
        </NeedsAttentionRowList>
      )}
    </NeedsAttentionSection>
  )
}
