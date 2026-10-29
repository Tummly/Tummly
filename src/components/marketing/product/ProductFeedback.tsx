import privateFeedbackIcon from "@/assets/svg/marketing/product/icons/private-feedback.svg"
import inTheirOwnWordsIcon from "@/assets/svg/marketing/product/icons/in-their-own-words.svg"
import marketingOptionalIcon from "@/assets/svg/marketing/product/icons/marketing-optional.svg"
import { ProductFeatureBand } from "@/components/marketing/product/ProductFeatureBand"
import { PRODUCT_FEEDBACK } from "@/content/marketing/productPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"

const feedbackPhoneImage = remoteSimpleImg("product/feedback-phone")

/** Figma Product Guest Form `5661:22688`. */
export function ProductFeedback() {
  return (
    <ProductFeatureBand
      headingId="product-feedback-heading"
      eyebrow={PRODUCT_FEEDBACK.eyebrow}
      title={PRODUCT_FEEDBACK.title}
      body={PRODUCT_FEEDBACK.body}
      points={PRODUCT_FEEDBACK.points}
      pointIcons={{
        "private-feedback": privateFeedbackIcon,
        "in-their-own-words": inTheirOwnWordsIcon,
        "marketing-optional": marketingOptionalIcon,
      }}
      media={{
        src: feedbackPhoneImage.src,
        srcSet: feedbackPhoneImage.srcSet,
        alt: PRODUCT_FEEDBACK.imageAlt,
        width: feedbackPhoneImage.width,
        height: feedbackPhoneImage.height,
        imageClassName: "inset-0 size-full object-contain",
      }}
    />
  )
}
