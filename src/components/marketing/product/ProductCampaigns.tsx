import bandImage from "@/assets/images/marketing/product/offers-campaigns.webp"
import makeOfferClearIcon from "@/assets/svg/marketing/product/icons/make-offer-clear.svg"
import prepareInvitationIcon from "@/assets/svg/marketing/product/icons/prepare-invitation.svg"
import reviewResponseIcon from "@/assets/svg/marketing/product/icons/review-response.svg"
import { ProductFeatureBand } from "@/components/marketing/product/ProductFeatureBand"
import { PRODUCT_CAMPAIGNS } from "@/content/marketing/productPage"

/** Figma Product Offers & campaigns `5661:23511`. */
export function ProductCampaigns() {
  return (
    <ProductFeatureBand
      headingId="product-campaigns-heading"
      eyebrow={PRODUCT_CAMPAIGNS.eyebrow}
      title={PRODUCT_CAMPAIGNS.title}
      body={PRODUCT_CAMPAIGNS.body}
      points={PRODUCT_CAMPAIGNS.points}
      pointIcons={{
        "make-offer-clear": makeOfferClearIcon,
        "prepare-invitation": prepareInvitationIcon,
        "review-response": reviewResponseIcon,
      }}
      media={{
        src: bandImage,
        alt: PRODUCT_CAMPAIGNS.imageAlt,
        width: 2160,
        height: 2160,
        imageClassName: "left-0 top-[0.06%] h-[116.62%] w-full",
      }}
      mediaFirst
    />
  )
}
