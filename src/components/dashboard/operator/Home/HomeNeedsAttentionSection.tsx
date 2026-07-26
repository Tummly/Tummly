import { useState } from "react"
import { ChevronDownIcon } from "lucide-react"

import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import {
  NEEDS_ATTENTION_EMPTY_COPY,
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

/** Figma Needs attention — component-owned empty shell. */
export function HomeNeedsAttentionSection() {
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
            <div className={OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS}>
              <p className={OPERATOR_HOME_EMPTY_TITLE_CLASS}>
                {NEEDS_ATTENTION_EMPTY_COPY}
              </p>
            </div>
          </AccordionContent>
        </AccordionItem>
      </Accordion>
    </section>
  )
}
