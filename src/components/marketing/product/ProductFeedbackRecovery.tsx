import bandImage from "@/assets/images/marketing/product/feedback-management.webp"
import findRightFeedbackIcon from "@/assets/svg/marketing/product/icons/find-right-feedback.svg"
import focusWhatMattersIcon from "@/assets/svg/marketing/product/icons/focus-what-matters.svg"
import prepareThoughtfulReplyIcon from "@/assets/svg/marketing/product/icons/prepare-thoughtful-reply.svg"
import { ProductFeatureBand } from "@/components/marketing/product/ProductFeatureBand"
import { PRODUCT_FEEDBACK_RECOVERY } from "@/content/marketing/productPage"

/** Figma Product Feedback management `5661:22714`. */
export function ProductFeedbackRecovery() {
  return (
    <ProductFeatureBand
      headingId="product-feedback-recovery-heading"
      eyebrow={PRODUCT_FEEDBACK_RECOVERY.eyebrow}
      title={PRODUCT_FEEDBACK_RECOVERY.title}
      body={PRODUCT_FEEDBACK_RECOVERY.body}
      points={PRODUCT_FEEDBACK_RECOVERY.points}
      pointIcons={{
        "find-right-feedback": findRightFeedbackIcon,
        "focus-what-matters": focusWhatMattersIcon,
        "prepare-thoughtful-reply": prepareThoughtfulReplyIcon,
      }}
      media={{
        src: bandImage,
        alt: PRODUCT_FEEDBACK_RECOVERY.imageAlt,
        width: 2160,
        height: 2160,
        imageClassName: "left-0 top-[0.14%] h-[116.62%] w-full",
      }}
      mediaFirst
    />
  )
}
