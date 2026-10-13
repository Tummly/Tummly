import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_FEEDBACK } from "@/content/marketing/productPage"
import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"

const feedbackPhoneImage = remoteSimpleUrl("product/feedback-phone")

/** Figma Product Feedback section `5661:22688`. */
export function ProductFeedback() {
  return (
    <ProductSplitFeatureSection
      headingId="product-feedback-heading"
      title={PRODUCT_FEEDBACK.title}
      body={PRODUCT_FEEDBACK.body}
      points={PRODUCT_FEEDBACK.points}
      imageSrc={feedbackPhoneImage}
      imageAlt={PRODUCT_FEEDBACK.phoneImageAlt}
      imageWidth={766}
      imageHeight={592}
    />
  )
}
