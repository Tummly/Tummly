import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_CAMPAIGNS } from "@/content/marketing/productPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"

const campaignsImage = remoteSimpleImg("product/campaigns")

/** Figma Product Campaigns & Offers `5661:23511` — media left. */
export function ProductCampaigns() {
  return (
    <ProductSplitFeatureSection
      headingId="product-campaigns-heading"
      eyebrow={PRODUCT_CAMPAIGNS.eyebrow}
      title={PRODUCT_CAMPAIGNS.title}
      body={PRODUCT_CAMPAIGNS.body}
      points={PRODUCT_CAMPAIGNS.points}
      imageSrc={campaignsImage.src}
      imageSrcSet={campaignsImage.srcSet}
      imageAlt={PRODUCT_CAMPAIGNS.imageAlt}
      imageWidth={campaignsImage.width}
      imageHeight={campaignsImage.height}
      mediaFirst
    />
  )
}
