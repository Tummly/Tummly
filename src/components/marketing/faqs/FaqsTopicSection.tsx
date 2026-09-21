import { Accordion } from "@/components/ui/accordion"
import { FaqsAccordionItem } from "@/components/marketing/faqs/FaqsAccordionItem"
import type { FaqsPageSection } from "@/content/marketing/faqsPage"
import {
  marketingSectionHeading,
  marketingSectionInset,
  marketingSectionPadding,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

type FaqsTopicSectionProps = {
  section: FaqsPageSection
  defaultOpenValue?: string
}

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
          "mx-auto flex w-full flex-col gap-8 sm:gap-10 lg:flex-row lg:items-start lg:gap-10 xl:gap-16 2xl:gap-24 min-[1728px]:gap-47.5",
          marketingSectionInset,
          marketingSectionPadding,
        )}
      >
        <header className="flex w-full shrink-0 flex-col gap-3 sm:max-w-sm lg:max-w-72 xl:max-w-99.5">
          <h2 className={cn("m-0 text-[#141414]", marketingSectionHeading)}>
            {section.title}
          </h2>
        </header>

        <Accordion
          type="single"
          collapsible
          className="min-w-0 flex-1 gap-8.5"
          defaultValue={sectionDefault}
        >
          {section.items.map((item) => (
            <FaqsAccordionItem
              key={item.id}
              value={`${section.id}::${item.id}`}
              question={item.question}
              answer={item.answer}
            />
          ))}
        </Accordion>
      </div>
    </section>
  )
}
