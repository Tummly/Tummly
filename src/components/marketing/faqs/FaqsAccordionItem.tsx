import { Link } from "react-router-dom"

import {
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"

/** Figma FAQ row: chevron chip #dbdbdb, 2px radius, 10px pad, 14px glyph. */
const faqTriggerClassName =
  "items-center rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer focus-visible:border-0 focus-visible:ring-0 **:data-[slot=accordion-trigger-icon]:box-content **:data-[slot=accordion-trigger-icon]:size-3.5 **:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:rounded-[2px] **:data-[slot=accordion-trigger-icon]:bg-[#dbdbdb] **:data-[slot=accordion-trigger-icon]:p-2.5 **:data-[slot=accordion-trigger-icon]:text-[#141414]"

const faqQuestionClassName =
  "m-0 min-w-0 flex-1 text-left font-sans text-lg font-medium leading-[normal] tracking-normal text-[#141414] sm:text-[22px]"

const faqContentClassName =
  "pt-4 pb-0 pr-6 text-base [&_a]:no-underline [&_p:not(:last-child)]:mb-0"

const faqAnswerClassName =
  "m-0 whitespace-pre-line font-sans text-base font-normal leading-5.25 tracking-normal text-[#141414]"

const faqCtaClassName =
  "m-0 font-sans text-base font-normal leading-5.25 tracking-normal text-[#141414] underline underline-offset-2 hover:opacity-80"

type FaqsAccordionItemProps = {
  value: string
  question: string
  answer: string
  cta?: {
    label: string
    to: string
  }
}

function FaqAnswerBody({
  answer,
  cta,
}: {
  answer: string
  cta?: FaqsAccordionItemProps["cta"]
}) {
  const blocks = answer.split(/\n\n/).filter((block) => block.length > 0)

  return (
    <div className="flex flex-col gap-5.25">
      {blocks.map((block, index) => (
        <p key={index} className={faqAnswerClassName}>
          {block}
        </p>
      ))}
      {cta != null ? (
        <p className="m-0">
          <Link to={cta.to} className={faqCtaClassName}>
            {cta.label}
          </Link>
        </p>
      ) : null}
    </div>
  )
}

export function FaqsAccordionItem({
  value,
  question,
  answer,
  cta,
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
        <FaqAnswerBody answer={answer} cta={cta} />
      </AccordionContent>
    </AccordionItem>
  )
}
