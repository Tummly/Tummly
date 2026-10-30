import { Plus } from "lucide-react"
import { Link } from "react-router-dom"

import {
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import { cn } from "@/lib/utils"

/** Figma FAQ row: chevron chip #dbdbdb, 2px radius, 10px pad, 14px glyph. */
const faqTriggerClassName =
  "items-center rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer focus-visible:border-0 focus-visible:ring-0 **:data-[slot=accordion-trigger-icon]:box-content **:data-[slot=accordion-trigger-icon]:size-3.5 **:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:rounded-[2px] **:data-[slot=accordion-trigger-icon]:bg-[#dbdbdb] **:data-[slot=accordion-trigger-icon]:p-2.5 **:data-[slot=accordion-trigger-icon]:text-[#141414]"

/**
 * QR materials FAQ toggle (`I6858:12617;6417:37022`): bare 28px plus, no chip.
 * Rotates 45° when the row is open so it reads as ×.
 */
const faqTriggerPlusClassName =
  "items-center rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer focus-visible:border-0 focus-visible:ring-0 **:data-[slot=accordion-trigger-icon]:size-7 **:data-[slot=accordion-trigger-icon]:text-[#141414]"

const faqPlusIcon = (
  <Plus
    aria-hidden
    className="size-[22px] transition-transform duration-200 group-data-[state=open]/accordion-trigger:rotate-45"
    strokeWidth={1.5}
  />
)

const faqQuestionClassName =
  "m-0 min-w-0 flex-1 text-left font-sans text-lg font-medium leading-[normal] tracking-normal text-[#141414] sm:text-[22px]"

const faqContentClassName =
  "pt-4 pb-0 pr-6 text-base [&_a]:no-underline [&_p:not(:last-child)]:mb-0"

const faqAnswerClassName =
  "m-0 whitespace-pre-line font-sans text-base font-normal leading-5.25 tracking-normal text-[#141414]"

/**
 * Pricing / Custom Plan FAQ (`4974:29501`, `6661:39170`): question 18px
 * SemiBold, answer 14/18, 30px from answer to the divider.
 */
const faqQuestionCompactClassName =
  "m-0 min-w-0 flex-1 text-left font-sans text-lg font-semibold leading-[normal] tracking-normal text-[#141414]"

const faqAnswerCompactClassName =
  "m-0 whitespace-pre-line font-sans text-sm font-normal leading-[18px] tracking-normal text-[#141414]"

const faqCtaClassName =
  "m-0 font-sans text-base font-normal leading-5.25 tracking-normal text-[#141414] underline underline-offset-2 hover:opacity-80"

type FaqsAccordionItemVariant = "default" | "compact"

/** `chip` = grey chevron chip (default). `plus` = bare plus that turns into ×. */
type FaqsAccordionItemToggle = "chip" | "plus"

type FaqsAccordionItemProps = {
  value: string
  question: string
  answer: string
  cta?: {
    label: string
    to: string
  }
  variant?: FaqsAccordionItemVariant
  toggle?: FaqsAccordionItemToggle
}

function FaqAnswerBody({
  answer,
  cta,
  variant,
}: {
  answer: string
  cta?: FaqsAccordionItemProps["cta"]
  variant: FaqsAccordionItemVariant
}) {
  const blocks = answer.split(/\n\n/).filter((block) => block.length > 0)
  const compact = variant === "compact"

  return (
    <div
      className={cn("flex flex-col", compact ? "gap-[18px]" : "gap-5.25")}
    >
      {blocks.map((block, index) => (
        <p
          key={index}
          className={compact ? faqAnswerCompactClassName : faqAnswerClassName}
        >
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
  variant = "default",
  toggle = "chip",
}: FaqsAccordionItemProps) {
  const compact = variant === "compact"
  const plusToggle = toggle === "plus"

  return (
    <AccordionItem
      value={value}
      className={cn(
        "border-0 border-b border-[#e0e0e0]",
        compact ? "pb-7.5" : "pb-3.5",
      )}
    >
      <AccordionTrigger
        className={plusToggle ? faqTriggerPlusClassName : faqTriggerClassName}
        icon={plusToggle ? faqPlusIcon : undefined}
      >
        <span
          className={
            compact ? faqQuestionCompactClassName : faqQuestionClassName
          }
        >
          {question}
        </span>
      </AccordionTrigger>
      <AccordionContent className={faqContentClassName}>
        <FaqAnswerBody answer={answer} cta={cta} variant={variant} />
      </AccordionContent>
    </AccordionItem>
  )
}
