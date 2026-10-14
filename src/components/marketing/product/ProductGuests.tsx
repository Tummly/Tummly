import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_GUESTS } from "@/content/marketing/productPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"

const guestsImage = remoteSimpleImg("product/guests")

/** Figma Product Guests section `5661:23456`. */
export function ProductGuests() {
  return (
    <ProductSplitFeatureSection
      headingId="product-guests-heading"
      eyebrow={PRODUCT_GUESTS.eyebrow}
      title={PRODUCT_GUESTS.title}
      body={PRODUCT_GUESTS.body}
      points={PRODUCT_GUESTS.points}
      imageSrc={guestsImage.src}
      imageSrcSet={guestsImage.srcSet}
      imageAlt={PRODUCT_GUESTS.imageAlt}
      imageWidth={guestsImage.width}
      imageHeight={guestsImage.height}
    />
  )
}
