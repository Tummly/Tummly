import { useState } from "react"
import { ChevronDownIcon, Diamond } from "lucide-react"

import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import { Button } from "@/components/ui/button"
import type { OperatorOffersNeedsAttentionView } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import type { OffersNeedsAttentionOverviewRow } from "@/lib/operatorOffers/buildOffersNeedsAttentionOverview"
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
  WARNING_ROW_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { GUESTS_PAGE_SECONDARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

const NEEDS_ATTENTION_ACCORDION_VALUE = "needs-attention"

type OffersNeedsAttentionSectionProps = {
  needsAttention: OperatorOffersNeedsAttentionView
  onRowCta: (row: OffersNeedsAttentionOverviewRow) => void
  onViewAll: () => void
}

/** Offers Needs attention — Home accordion chrome; rule rows when facts exist. */
export function OffersNeedsAttentionSection({
  needsAttention,
  onRowCta,
  onViewAll,
}: OffersNeedsAttentionSectionProps) {
  const [openValues, setOpenValues] = useState<string[]>([
    NEEDS_ATTENTION_ACCORDION_VALUE,
  ])
  const isOpen = openValues.includes(NEEDS_ATTENTION_ACCORDION_VALUE)

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
                {needsAttention.title}
              </h2>
              <p className={OPERATOR_HOME_SUBTITLE_CLASS}>
                {needsAttention.subtitle}
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
            {needsAttention.isEmpty ? (
              <div className={OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS}>
                <p className={OPERATOR_HOME_EMPTY_TITLE_CLASS}>
                  {needsAttention.emptyCopy}
                </p>
              </div>
            ) : (
              <div className="flex flex-col gap-3">
                {needsAttention.rows.map((row) => (
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
                    <Button
                      type="button"
                      variant="op-tertiary"
                      className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                      onClick={() => {
                        onRowCta(row)
                      }}
                    >
                      {row.ctaLabel}
                    </Button>
                  </div>
                ))}
                {needsAttention.showViewAll ? (
                  <div className="flex justify-end pt-1">
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
              </div>
            )}
          </AccordionContent>
        </AccordionItem>
      </Accordion>
    </section>
  )
}
