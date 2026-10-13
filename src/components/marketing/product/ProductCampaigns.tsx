import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_CAMPAIGNS } from "@/content/marketing/productPage"
import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"

const campaignsImage = remoteSimpleUrl("product/campaigns")

/** Figma Product Campaigns & Offers `5661:23511` — media left. */
export function ProductCampaigns() {
  return (
    <ProductSplitFeatureSection
      headingId="product-campaigns-heading"
      eyebrow={PRODUCT_CAMPAIGNS.eyebrow}
      title={PRODUCT_CAMPAIGNS.title}
      body={PRODUCT_CAMPAIGNS.body}
      points={PRODUCT_CAMPAIGNS.points}
      imageSrc={campaignsImage}
      imageAlt={PRODUCT_CAMPAIGNS.imageAlt}
      imageWidth={766}
      imageHeight={648}
      mediaFirst
    />
  )
}
