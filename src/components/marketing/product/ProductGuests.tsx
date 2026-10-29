import bandImage from "@/assets/images/marketing/product/guest-context.webp"
import understandPreviousFeedbackIcon from "@/assets/svg/marketing/product/icons/understand-previous-feedback.svg"
import reviewOfferActivityIcon from "@/assets/svg/marketing/product/icons/review-offer-activity.svg"
import channelChoicesIcon from "@/assets/svg/marketing/product/icons/channel-choices.svg"
import { ProductFeatureBand } from "@/components/marketing/product/ProductFeatureBand"
import { PRODUCT_GUESTS } from "@/content/marketing/productPage"

/** Figma Product Guest context `5661:23065`. */
export function ProductGuests() {
  return (
    <ProductFeatureBand
      headingId="product-guests-heading"
      eyebrow={PRODUCT_GUESTS.eyebrow}
      title={PRODUCT_GUESTS.title}
      body={PRODUCT_GUESTS.body}
      points={PRODUCT_GUESTS.points}
      pointIcons={{
        "understand-previous-feedback": understandPreviousFeedbackIcon,
        "review-offer-activity": reviewOfferActivityIcon,
        "channel-choices": channelChoicesIcon,
      }}
      media={{
        src: bandImage,
        alt: PRODUCT_GUESTS.imageAlt,
        width: 2160,
        height: 2160,
        imageClassName: "left-[0.06%] top-[-8.31%] h-[116.62%] w-full",
      }}
    />
  )
}
