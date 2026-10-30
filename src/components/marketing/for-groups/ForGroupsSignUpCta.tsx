import { forGroupsSignUpImage } from "@/assets/for-groups-images"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { FOR_GROUPS_SIGN_UP_CTA } from "@/content/marketing/forGroupsPage"

/**
 * For Groups bottom CTA — Figma Sign up `6405:36178` (1668×615).
 * Editorial variant (dark type on a heat-map plate), "read" legal note.
 */
export function ForGroupsSignUpCta() {
  return (
    <MarketingSignUpCta
      content={FOR_GROUPS_SIGN_UP_CTA}
      variant="editorial"
      editorialBackgroundSrc={forGroupsSignUpImage.src}
      editorialBackgroundSrcSet={forGroupsSignUpImage.srcSet}
      editorialBackgroundWidth={forGroupsSignUpImage.width}
      editorialBackgroundHeight={forGroupsSignUpImage.height}
      editorialAspectClass="lg:aspect-[1668/615] lg:min-h-[615px]"
      bodyClassName="md:max-w-[560px]"
      plateClassName="rounded-[12px]"
      legalNoteClassName="leading-[18px]"
      socialButtonClassName="bg-[#fafafa] hover:bg-[#fafafa]/90"
    />
  )
}
