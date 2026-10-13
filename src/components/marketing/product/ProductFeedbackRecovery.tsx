import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_FEEDBACK_RECOVERY } from "@/content/marketing/productPage"
import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"

const feedbackRecoveryImage = remoteSimpleUrl("product/feedback-recovery")

/** Figma Product Feedback recovery `5661:23401`. */
export function ProductFeedbackRecovery() {
  return (
    <ProductSplitFeatureSection
      headingId="product-feedback-recovery-heading"
      eyebrow={PRODUCT_FEEDBACK_RECOVERY.eyebrow}
      title={PRODUCT_FEEDBACK_RECOVERY.title}
      body={PRODUCT_FEEDBACK_RECOVERY.body}
      points={PRODUCT_FEEDBACK_RECOVERY.points}
      imageSrc={feedbackRecoveryImage}
      imageAlt={PRODUCT_FEEDBACK_RECOVERY.imageAlt}
      imageWidth={766}
      imageHeight={647}
    />
  )
}
