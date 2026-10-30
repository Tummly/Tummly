import qrCounterDisplayImage from "@/assets/images/marketing/home/qr-counter-display.webp"
import {
  ProductCaptureMaterialsPanel,
  type CaptureMaterialImage,
} from "@/components/marketing/product/ProductCaptureMaterialsPanel"
import { HOME_QR_MATERIALS } from "@/content/marketing/homePage"
import { cn } from "@/lib/utils"

/** Figma `4902:19649` photo for the first format (1448×1086). */
const HOME_QR_MATERIAL_IMAGES: Readonly<Record<string, CaptureMaterialImage>> = {
  "counter-display": {
    src: qrCounterDisplayImage,
    width: 1448,
    height: 1086,
  },
}

type HomeQrMaterialsProps = {
  /** Intro copy; the format list stays Home's. Defaults to the Home copy. */
  content?: {
    /** Omit to hide the eyebrow (QR materials page `5661:19364`). */
    eyebrow?: string
    title: string
    body: string
  }
  /** Merged onto the padded wrapper, e.g. `lg:pb-0` when the next section supplies the gap. */
  className?: string
}

/**
 * Home QR materials — Figma `4902:19649` / mobile `6241:18095`.
 * Same Capture materials panel as Product, with the format list and
 * intro copy above it. Also rendered on How it works and QR materials.
 */
export function HomeQrMaterials({
  content = HOME_QR_MATERIALS,
  className,
}: HomeQrMaterialsProps) {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="home-qr-materials-heading"
    >
      <div
        className={cn(
          "mx-auto w-full",
          /* Mobile Figma `6241:18093`: 20px white pad around rounded grey card.
           * That pad is the gap before Built for restaurants (`6241:19397`).
           * Desktop: 60px content inset. */
          "px-0 py-5 lg:px-[60px] lg:py-[70px]",
          className,
        )}
      >
        <ProductCaptureMaterialsPanel
          intro={{
            eyebrow: content.eyebrow,
            title: content.title,
            body: content.body,
            titleId: "home-qr-materials-heading",
            eyebrowClassName: "text-[#4a4a4a]",
            bodyClassName: "lg:max-w-[659px]",
          }}
          materials={HOME_QR_MATERIALS.items}
          imageOverrides={HOME_QR_MATERIAL_IMAGES}
        />
      </div>
    </section>
  )
}
