import qrCounterDisplayImage from "@/assets/images/marketing/product/qr-counter-display.webp"
import {
  ProductCaptureMaterialsPanel,
  type CaptureMaterialImage,
} from "@/components/marketing/product/ProductCaptureMaterialsPanel"
import { PRODUCT_QR_MATERIALS } from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

/** Figma `5661:22668` photo for the first format (1448×1086). */
const PRODUCT_QR_MATERIAL_IMAGES: Readonly<
  Record<string, CaptureMaterialImage>
> = {
  "counter-display": {
    src: qrCounterDisplayImage,
    width: 1448,
    height: 1086,
  },
}

/**
 * Figma Product QR materials `5661:22668`.
 * Same panel as Home QR materials: intro copy above the format list, photo on
 * the right. Other formats keep the published Capture photos.
 */
export function ProductCapture() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="product-capture-heading"
    >
      <div
        className={cn("mx-auto w-full", "px-0 py-5 lg:px-[60px] lg:py-[70px]")}
      >
        <ProductCaptureMaterialsPanel
          intro={{
            eyebrow: PRODUCT_QR_MATERIALS.eyebrow,
            title: PRODUCT_QR_MATERIALS.title,
            body: PRODUCT_QR_MATERIALS.body,
            titleId: "product-capture-heading",
            eyebrowClassName: "text-[#4a4a4a]",
            bodyClassName: "lg:max-w-[563px]",
          }}
          materials={PRODUCT_QR_MATERIALS.items}
          imageOverrides={PRODUCT_QR_MATERIAL_IMAGES}
        />
      </div>
    </section>
  )
}
