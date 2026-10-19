import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { FOR_GROUPS_SIGN_UP_CTA } from "@/content/marketing/forGroupsPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"

const signupCtaBg = remoteSimpleImg("for-groups/signup-cta-bg")

/**
 * For Groups bottom CTA (Figma Sign up `6405:36178`).
 * Editorial variant with neon lime marble from Blob.
 */
export function ForGroupsSignUpCta() {
  return (
    <MarketingSignUpCta
      content={FOR_GROUPS_SIGN_UP_CTA}
      variant="editorial"
      editorialBackgroundSrc={signupCtaBg.src}
      editorialBackgroundSrcSet={signupCtaBg.srcSet}
      editorialBackgroundWidth={signupCtaBg.width}
      editorialBackgroundHeight={signupCtaBg.height}
      editorialAspectClass="lg:aspect-[1668/565]"
    />
  )
}
