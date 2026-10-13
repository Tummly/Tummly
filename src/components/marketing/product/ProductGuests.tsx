import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_GUESTS } from "@/content/marketing/productPage"
import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"

const guestsImage = remoteSimpleUrl("product/guests")

/** Figma Product Guests section `5661:23456`. */
export function ProductGuests() {
  return (
    <ProductSplitFeatureSection
      headingId="product-guests-heading"
      eyebrow={PRODUCT_GUESTS.eyebrow}
      title={PRODUCT_GUESTS.title}
      body={PRODUCT_GUESTS.body}
      points={PRODUCT_GUESTS.points}
      imageSrc={guestsImage}
      imageAlt={PRODUCT_GUESTS.imageAlt}
      imageWidth={766}
      imageHeight={648}
    />
  )
}
