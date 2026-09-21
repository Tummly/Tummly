import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import {
  MANAGE_PLAN_COPY,
  MANAGE_PLAN_FAQ_ANSWER_CLASS,
  MANAGE_PLAN_FAQ_QUESTION_CLASS,
  MANAGE_PLAN_FAQ_TRIGGER_CLASS,
  MANAGE_PLAN_SECTION_HEADING_CLASS,
  buildManagePlanFaqItems,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

export function ManagePlanFaqSection({
  vatRateBps = 0,
}: {
  /** Catalog / API rate in basis points; 0 omits “+ VAT” FAQ copy. */
  vatRateBps?: number
}) {
  const faqItems = buildManagePlanFaqItems({ vatRateBps })

  return (
    <section className="flex flex-col gap-10 pb-17.5 lg:flex-row lg:items-start lg:gap-15">
      <h2
        className={cn(
          MANAGE_PLAN_SECTION_HEADING_CLASS,
          "max-w-137 shrink-0 py-5 lg:sticky lg:top-6"
        )}
      >
        {MANAGE_PLAN_COPY.faqHeading}
      </h2>

      <Accordion
        type="single"
        collapsible
        defaultValue={faqItems[0]?.id}
        className="min-w-0 flex-1 pt-5"
      >
        {faqItems.map((item, index) => (
          <AccordionItem
            key={item.id}
            value={item.id}
            className={cn(
              "border-0",
              index > 0 && "pt-7.5",
              index < faqItems.length - 1 &&
                "border-b border-border pb-7.5"
            )}
          >
            <AccordionTrigger className={MANAGE_PLAN_FAQ_TRIGGER_CLASS}>
              <span className={MANAGE_PLAN_FAQ_QUESTION_CLASS}>
                {item.question}
              </span>
            </AccordionTrigger>
            <AccordionContent className="pt-4 pb-0">
              <div className="flex flex-col gap-4">
                {item.answerParagraphs.map((paragraph) => (
                  <p
                    key={paragraph}
                    className={MANAGE_PLAN_FAQ_ANSWER_CLASS}
                  >
                    {paragraph}
                  </p>
                ))}
              </div>
            </AccordionContent>
          </AccordionItem>
        ))}
      </Accordion>
    </section>
  )
}
