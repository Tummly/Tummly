import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_REPORTS } from "@/content/marketing/productPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"

const reportsImage = remoteSimpleImg("product/reports")

/** Figma Product Reports section `5661:23570`. */
export function ProductReports() {
  return (
    <ProductSplitFeatureSection
      headingId="product-reports-heading"
      eyebrow={PRODUCT_REPORTS.eyebrow}
      title={PRODUCT_REPORTS.title}
      body={PRODUCT_REPORTS.body}
      points={PRODUCT_REPORTS.points}
      imageSrc={reportsImage.src}
      imageSrcSet={reportsImage.srcSet}
      imageAlt={PRODUCT_REPORTS.imageAlt}
      imageWidth={reportsImage.width}
      imageHeight={reportsImage.height}
    />
  )
}
