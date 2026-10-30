import bandImage from "@/assets/images/marketing/product/reports.webp"
import spotRecurringFeedbackIcon from "@/assets/svg/marketing/product/icons/spot-recurring-feedback.svg"
import measureCampaignResponseIcon from "@/assets/svg/marketing/product/icons/measure-campaign-response.svg"
import makeInformedDecisionsIcon from "@/assets/svg/marketing/product/icons/make-informed-decisions.svg"
import { ProductFeatureBand } from "@/components/marketing/product/ProductFeatureBand"
import { PRODUCT_REPORTS } from "@/content/marketing/productPage"

/** Figma Product Reports `5661:23587`. */
export function ProductReports() {
  return (
    <ProductFeatureBand
      headingId="product-reports-heading"
      eyebrow={PRODUCT_REPORTS.eyebrow}
      title={PRODUCT_REPORTS.title}
      body={PRODUCT_REPORTS.body}
      points={PRODUCT_REPORTS.points}
      pointIcons={{
        "spot-recurring-feedback": spotRecurringFeedbackIcon,
        "measure-campaign-response": measureCampaignResponseIcon,
        "make-informed-decisions": makeInformedDecisionsIcon,
      }}
      media={{
        src: bandImage,
        alt: PRODUCT_REPORTS.imageAlt,
        width: 2160,
        height: 2160,
        imageClassName: "left-0 top-[-1.7%] h-[116.62%] w-full",
      }}
    />
  )
}
