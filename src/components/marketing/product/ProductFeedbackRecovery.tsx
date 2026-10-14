import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_FEEDBACK_RECOVERY } from "@/content/marketing/productPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"

const feedbackRecoveryImage = remoteSimpleImg("product/feedback-recovery")

/** Figma Product Feedback recovery `5661:23401`. */
export function ProductFeedbackRecovery() {
  return (
    <ProductSplitFeatureSection
      headingId="product-feedback-recovery-heading"
      eyebrow={PRODUCT_FEEDBACK_RECOVERY.eyebrow}
      title={PRODUCT_FEEDBACK_RECOVERY.title}
      body={PRODUCT_FEEDBACK_RECOVERY.body}
      points={PRODUCT_FEEDBACK_RECOVERY.points}
      imageSrc={feedbackRecoveryImage.src}
      imageSrcSet={feedbackRecoveryImage.srcSet}
      imageAlt={PRODUCT_FEEDBACK_RECOVERY.imageAlt}
      imageWidth={feedbackRecoveryImage.width}
      imageHeight={feedbackRecoveryImage.height}
    />
  )
}
