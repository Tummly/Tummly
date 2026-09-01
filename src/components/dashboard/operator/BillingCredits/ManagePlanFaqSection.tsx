import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import {
  MANAGE_PLAN_COPY,
  MANAGE_PLAN_FAQ_ANSWER_CLASS,
  MANAGE_PLAN_FAQ_ITEMS,
  MANAGE_PLAN_FAQ_QUESTION_CLASS,
  MANAGE_PLAN_SECTION_HEADING_CLASS,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

/** Figma FAQ row: 16px Q→A gap; 30px between items; chevron chip p-10 / 14px icon. */
const faqTriggerClassName =
  "items-center gap-4 rounded-none border-0 py-0 hover:no-underline focus-visible:border-0 focus-visible:ring-0 **:data-[slot=accordion-trigger-icon]:box-content **:data-[slot=accordion-trigger-icon]:size-3.5 **:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:rounded-[2px] **:data-[slot=accordion-trigger-icon]:bg-[#212121] **:data-[slot=accordion-trigger-icon]:p-2.5 **:data-[slot=accordion-trigger-icon]:text-white"

export function ManagePlanFaqSection() {
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
        defaultValue={MANAGE_PLAN_FAQ_ITEMS[0]?.id}
        className="min-w-0 flex-1 pt-5"
      >
        {MANAGE_PLAN_FAQ_ITEMS.map((item, index) => (
          <AccordionItem
            key={item.id}
            value={item.id}
            className={cn(
              "border-0",
              index > 0 && "pt-7.5",
              index < MANAGE_PLAN_FAQ_ITEMS.length - 1 &&
                "border-b border-border pb-7.5"
            )}
          >
            <AccordionTrigger className={faqTriggerClassName}>
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
