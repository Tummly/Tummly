import { ProductCaptureMaterialsPanel } from "@/components/marketing/product/ProductCaptureMaterialsPanel"
import { HOME_QR_MATERIALS } from "@/content/marketing/homePage"
import { cn } from "@/lib/utils"

/**
 * Home QR materials — Figma `4902:19649` / mobile `6241:18095`.
 * Same Capture materials panel as Product; intro copy sits above the list.
 */
export function HomeQrMaterials() {
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
        )}
      >
        <ProductCaptureMaterialsPanel
          intro={{
            eyebrow: HOME_QR_MATERIALS.eyebrow,
            title: HOME_QR_MATERIALS.title,
            body: HOME_QR_MATERIALS.body,
            titleId: "home-qr-materials-heading",
          }}
        />
      </div>
    </section>
  )
}
