import { Accordion } from "@/components/ui/accordion"
import { FaqsAccordionItem } from "@/components/marketing/faqs/FaqsAccordionItem"
import type { FaqsPageSection } from "@/content/marketing/faqsPage"
import {
  marketingChromeContentInset,
  marketingSectionHeading,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

type FaqsTopicSectionProps = {
  section: FaqsPageSection
  defaultOpenValue?: string
}

/** Figma topic stack: pad 50/70/60, 60px column gap, 30px accordion gap. */
export function FaqsTopicSection({
  section,
  defaultOpenValue,
}: FaqsTopicSectionProps) {
  const sectionDefault =
    defaultOpenValue?.startsWith(`${section.id}::`) === true
      ? defaultOpenValue
      : undefined

  return (
    <section
      id={section.id}
      className="w-full scroll-mt-44 bg-white"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-8 sm:gap-10 lg:flex-row lg:items-start lg:gap-15",
          marketingChromeContentInset,
          "pt-12.5 pb-17.5",
        )}
      >
        <header className="flex w-full shrink-0 flex-col gap-3 lg:sticky lg:top-44 lg:w-137 lg:max-w-137 lg:pt-5">
          <h2 className={cn("m-0 text-[#141414]", marketingSectionHeading)}>
            {section.title}
          </h2>
        </header>

        <Accordion
          type="single"
          collapsible
          className="min-w-0 flex-1 gap-7.5 lg:pt-5"
          defaultValue={sectionDefault}
        >
          {section.items.map((item) => (
            <FaqsAccordionItem
              key={item.id}
              value={`${section.id}::${item.id}`}
              question={item.question}
              answer={item.answer}
              cta={item.cta}
            />
          ))}
        </Accordion>
      </div>
    </section>
  )
}
