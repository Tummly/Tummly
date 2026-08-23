import { useState } from "react"

import {
  NeedsAttentionEmpty,
  NeedsAttentionRow,
  NeedsAttentionRowList,
  NeedsAttentionSection,
} from "@/components/dashboard/operator/NeedsAttentionSection"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import type {
  HomeNeedsAttentionCtaKind,
  HomeNeedsAttentionItem,
  HomeNeedsAttentionProjection,
} from "@/lib/operatorHome/buildHomeNeedsAttention"
import {
  NEEDS_ATTENTION_EMPTY_COPY,
  NEEDS_ATTENTION_VIEW_ALL_LABEL,
  resolveHomeNeedsAttentionSectionBody,
  type HomeNeedsAttentionLoadStatus,
} from "@/lib/operatorHome/homeNeedsAttentionSectionPresentation"
import { GUESTS_PAGE_SECONDARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import { NEEDS_ATTENTION_VIEW_ALL_ROW_CLASS } from "@/lib/operatorHome/operatorHomeSectionPresentation"

export type HomeNeedsAttentionSectionProps = {
  loadStatus: HomeNeedsAttentionLoadStatus
  projection: HomeNeedsAttentionProjection | null
  errorMessage?: string | null
  onRetry?: () => void
  onCta: (
    item: HomeNeedsAttentionItem,
    ctaKind: HomeNeedsAttentionCtaKind
  ) => void
}

/** Home Needs attention — load / empty / error+Retry / shared Figma rows. */
export function HomeNeedsAttentionSection({
  loadStatus,
  projection,
  errorMessage = null,
  onRetry,
  onCta,
}: HomeNeedsAttentionSectionProps) {
  const [expanded, setExpanded] = useState(false)
  const body = resolveHomeNeedsAttentionSectionBody({
    loadStatus,
    projection,
    errorMessage,
    expanded,
  })

  return (
    <NeedsAttentionSection
      title="Needs attention"
      subtitle="Review issues that may require action."
    >
      {body.mode === "loading" ? (
        <div className="flex min-h-[120px] items-center justify-center">
          <Spinner
            className="size-6"
            aria-label="Loading Needs attention"
          />
        </div>
      ) : null}

      {body.mode === "error" ? (
        <div className="px-6 py-10 text-center">
          <p className="text-sm text-destructive">{body.message}</p>
          {onRetry ? (
            <Button
              type="button"
              variant="link"
              size="link-sm"
              className="mt-3 font-medium underline"
              onClick={onRetry}
            >
              Retry
            </Button>
          ) : null}
        </div>
      ) : null}

      {body.mode === "empty" ? (
        <NeedsAttentionEmpty copy={NEEDS_ATTENTION_EMPTY_COPY} />
      ) : null}

      {body.mode === "rows" ? (
        <NeedsAttentionRowList>
          {body.rows.map((row) => (
            <NeedsAttentionRow
              key={row.id}
              title={row.title}
              body={row.body}
              metaLine={row.metaLine}
              tone={row.metaKind}
              icon={row.sourceKind === "offer" ? "tag" : undefined}
              actions={row.ctas.map((cta) => ({
                key: cta.kind,
                label: cta.label,
                onClick: () => {
                  onCta(row, cta.kind)
                },
              }))}
            />
          ))}
          {body.showViewAll ? (
            <div className={NEEDS_ATTENTION_VIEW_ALL_ROW_CLASS}>
              <Button
                type="button"
                variant="op-tertiary"
                className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                onClick={() => {
                  setExpanded(true)
                }}
              >
                {NEEDS_ATTENTION_VIEW_ALL_LABEL}
              </Button>
            </div>
          ) : null}
        </NeedsAttentionRowList>
      ) : null}
    </NeedsAttentionSection>
  )
}
