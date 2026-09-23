import { useEffect, useState } from "react"

import Footer from "@/components/home/Footer"
import { FaqsPageHero } from "@/components/marketing/faqs/FaqsPageHero"
import { FaqsSignUpCta } from "@/components/marketing/faqs/FaqsSignUpCta"
import { FaqsTopicSection } from "@/components/marketing/faqs/FaqsTopicSection"
import {
  FAQS_PAGE_DEFAULT_OPEN_VALUE,
  FAQS_PAGE_SECTIONS,
  filterFaqsPageSections,
} from "@/content/marketing/faqsPage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { warmCtaLaunchBg } from "@/lib/prefetchCtaLaunchBg"
import { cn } from "@/lib/utils"

export default function FaqsPage() {
  const [searchQuery, setSearchQuery] = useState("")
  const sections = filterFaqsPageSections(FAQS_PAGE_SECTIONS, searchQuery)

  useEffect(() => {
    warmCtaLaunchBg({ priority: true })
  }, [])

  return (
    <>
      <FaqsPageHero
        searchQuery={searchQuery}
        onSearchQueryChange={setSearchQuery}
      />
      {sections.length === 0 ? (
        <section
          className={cn("w-full bg-white py-16", marketingChromeContentInset)}
        >
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
      <FaqsSignUpCta />
      <Footer />
    </>
  )
}
