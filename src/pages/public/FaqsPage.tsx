import { useState } from "react"

import CTALaunch from "@/components/home/CTALaunch"
import Footer from "@/components/home/Footer"
import { FaqsPageHero } from "@/components/marketing/faqs/FaqsPageHero"
import { FaqsTopicSection } from "@/components/marketing/faqs/FaqsTopicSection"
import {
  FAQS_PAGE_DEFAULT_OPEN_VALUE,
  FAQS_PAGE_SECTIONS,
  filterFaqsPageSections,
} from "@/content/marketing/faqsPage"

export default function FaqsPage() {
  const [searchQuery, setSearchQuery] = useState("")
  const sections = filterFaqsPageSections(FAQS_PAGE_SECTIONS, searchQuery)

  return (
    <>
      <FaqsPageHero
        searchQuery={searchQuery}
        onSearchQueryChange={setSearchQuery}
      />
      {sections.length === 0 ? (
        <section className="w-full bg-white px-[25px] py-16 lg:px-16 xl:px-45">
          <p className="m-0 text-base text-[#141414]">
            No FAQs match “{searchQuery.trim()}”.
          </p>
        </section>
      ) : (
        sections.map((section) => (
          <FaqsTopicSection
            key={section.id}
            section={section}
            defaultOpenValue={
              searchQuery.trim().length === 0
                ? FAQS_PAGE_DEFAULT_OPEN_VALUE
                : `${section.id}::${section.items[0]!.id}`
            }
          />
        ))
      )}
      <CTALaunch />
      <Footer />
    </>
  )
}
