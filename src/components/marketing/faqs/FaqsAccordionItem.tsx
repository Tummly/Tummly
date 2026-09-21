import {
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"

/** Figma FAQ row: light-gray chevron chip (#eaeaea), dark glyph — not homepage dark circle. */
const faqTriggerClassName =
  "items-center rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer focus-visible:border-0 focus-visible:ring-0 **:data-[slot=accordion-trigger-icon]:box-content **:data-[slot=accordion-trigger-icon]:size-3.5 **:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:rounded-[4px] **:data-[slot=accordion-trigger-icon]:bg-[#eaeaea] **:data-[slot=accordion-trigger-icon]:p-2 **:data-[slot=accordion-trigger-icon]:text-[#141414]"

const faqQuestionClassName =
  "m-0 min-w-0 flex-1 text-left font-sans text-lg font-semibold leading-[normal] tracking-normal text-[#141414] sm:text-[22px]"

const faqContentClassName = "pt-3 pb-0 pr-6"

const faqAnswerClassName =
  "m-0 font-sans text-base font-normal leading-5.5 tracking-normal text-[#141414]"

type FaqsAccordionItemProps = {
  value: string
  question: string
  answer: string
}

export function FaqsAccordionItem({
  value,
  question,
  answer,
}: FaqsAccordionItemProps) {
  return (
    <AccordionItem
      value={value}
      className="border-0 border-b border-[#e0e0e0] pb-3.5"
    >
      <AccordionTrigger className={faqTriggerClassName}>
        <span className={faqQuestionClassName}>{question}</span>
      </AccordionTrigger>
      <AccordionContent className={faqContentClassName}>
        <p className={faqAnswerClassName}>{answer}</p>
      </AccordionContent>
    </AccordionItem>
  )
}
