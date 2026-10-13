import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_FEEDBACK } from "@/content/marketing/productPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"

const feedbackPhoneImage = remoteSimpleImg("product/feedback-phone")

/** Figma Product Feedback section `5661:22688`. */
export function ProductFeedback() {
  return (
    <ProductSplitFeatureSection
      headingId="product-feedback-heading"
      title={PRODUCT_FEEDBACK.title}
      body={PRODUCT_FEEDBACK.body}
      points={PRODUCT_FEEDBACK.points}
      imageSrc={feedbackPhoneImage.src}
      imageSrcSet={feedbackPhoneImage.srcSet}
      imageAlt={PRODUCT_FEEDBACK.phoneImageAlt}
      imageWidth={feedbackPhoneImage.width}
      imageHeight={feedbackPhoneImage.height}
    />
  )
}
