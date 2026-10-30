import { ProductFaqs } from "@/components/marketing/product/ProductFaqs"
import { FOR_GROUPS_FAQS } from "@/content/marketing/forGroupsPage"

/**
 * Figma For Groups FAQs `5661:22571` — the Product accordion with a 780px list.
 * Every answer starts open, as the frame shows.
 */
export function ForGroupsFaqs() {
  return (
    <ProductFaqs
      content={FOR_GROUPS_FAQS}
      headingId="for-groups-faqs-heading"
      listClassName="lg:max-w-[780px]"
    />
  )
}
