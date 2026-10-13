import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_REPORTS } from "@/content/marketing/productPage"
import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"

const reportsImage = remoteSimpleUrl("product/reports")

/** Figma Product Reports section `5661:23570`. */
export function ProductReports() {
  return (
    <ProductSplitFeatureSection
      headingId="product-reports-heading"
      eyebrow={PRODUCT_REPORTS.eyebrow}
      title={PRODUCT_REPORTS.title}
      body={PRODUCT_REPORTS.body}
      points={PRODUCT_REPORTS.points}
      imageSrc={reportsImage}
      imageAlt={PRODUCT_REPORTS.imageAlt}
      imageWidth={766}
      imageHeight={648}
    />
  )
}
