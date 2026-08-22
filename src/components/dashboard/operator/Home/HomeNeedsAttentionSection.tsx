import { useState } from "react"
import { ChevronDownIcon, Diamond } from "lucide-react"

import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
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
  WARNING_ROW_CLASS,
  type HomeNeedsAttentionLoadStatus,
} from "@/lib/operatorHome/homeNeedsAttentionSectionPresentation"
import { GUESTS_PAGE_SECONDARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import {
  OPERATOR_HOME_CARD_PADDED_CLASS,
  OPERATOR_HOME_CHROME_BUTTON_CLASS,
  OPERATOR_HOME_CHROME_ICON_CLASS,
  OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS,
  OPERATOR_HOME_EMPTY_TITLE_CLASS,
  OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS,
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_HEADER_ROW_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { cn } from "@/lib/utils"

const NEEDS_ATTENTION_ACCORDION_VALUE = "needs-attention"

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

/** Figma Needs attention — loaded / empty / error+Retry / warning rows. */
export function HomeNeedsAttentionSection({
  loadStatus,
  projection,
  errorMessage = null,
  onRetry,
  onCta,
}: HomeNeedsAttentionSectionProps) {
  const [openValues, setOpenValues] = useState<string[]>([
    NEEDS_ATTENTION_ACCORDION_VALUE,
  ])
  const [expanded, setExpanded] = useState(false)
  const isOpen = openValues.includes(NEEDS_ATTENTION_ACCORDION_VALUE)
  const body = resolveHomeNeedsAttentionSectionBody({
    loadStatus,
    projection,
    errorMessage,
    expanded,
  })

  return (
    <section className={OPERATOR_HOME_CARD_PADDED_CLASS}>
      <Accordion
        type="multiple"
        value={openValues}
        onValueChange={setOpenValues}
      >
        <AccordionItem
          value={NEEDS_ATTENTION_ACCORDION_VALUE}
          className="border-none"
        >
          <AccordionTrigger
            className={cn(
              OPERATOR_HOME_HEADER_ROW_CLASS,
              "cursor-pointer py-0 hover:no-underline **:data-[slot=accordion-trigger-icon]:hidden"
            )}
          >
            <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
              <h2 className={OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS}>
                Needs attention
              </h2>
              <p className={OPERATOR_HOME_SUBTITLE_CLASS}>
                Review issues that may require action.
              </p>
            </div>
            <span className={OPERATOR_HOME_CHROME_BUTTON_CLASS} aria-hidden>
              <ChevronDownIcon
                className={cn(
                  OPERATOR_HOME_CHROME_ICON_CLASS,
                  "transition-transform duration-200 motion-reduce:transition-none",
                  isOpen && "rotate-180"
                )}
              />
            </span>
          </AccordionTrigger>

          <AccordionContent className="pt-6 pb-0 sm:pt-8 md:pt-10">
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
              <div className={OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS}>
                <p className={OPERATOR_HOME_EMPTY_TITLE_CLASS}>
                  {NEEDS_ATTENTION_EMPTY_COPY}
                </p>
              </div>
            ) : null}

            {body.mode === "rows" ? (
              <div className="flex flex-col gap-3">
                {body.rows.map((row) => (
                  <div key={row.id} className={WARNING_ROW_CLASS}>
                    <Diamond
                      className="size-4 shrink-0 text-op-action-primary"
                      aria-hidden
                    />
                    <div className="flex min-w-0 flex-1 flex-col gap-1.5">
                      <p className="m-0 text-base font-semibold leading-6 tracking-[-0.4px] text-op-card-title-color">
                        {row.title}
                      </p>
                      <p className="m-0 text-sm font-medium leading-normal text-op-card-title-color">
                        {row.body}
                      </p>
                      <p className="m-0 text-xs font-medium leading-normal tracking-[-0.4px] text-op-card-subtitle-color">
                        {row.metaLine}
                      </p>
                    </div>
                    <div className="flex shrink-0 flex-wrap items-center gap-2">
                      {row.ctas.map((cta) => (
                        <Button
                          key={cta.kind}
                          type="button"
                          variant="op-tertiary"
                          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                          onClick={() => {
                            onCta(row, cta.kind)
                          }}
                        >
                          {cta.label}
                        </Button>
                      ))}
                    </div>
                  </div>
                ))}
                {body.showViewAll ? (
                  <div className="flex justify-end pt-1">
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
              </div>
            ) : null}
          </AccordionContent>
        </AccordionItem>
      </Accordion>
    </section>
  )
}
