import { ProductCaptureMaterialsPanel } from "@/components/marketing/product/ProductCaptureMaterialsPanel"
import { HOME_QR_MATERIALS } from "@/content/marketing/homePage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Home QR materials — Figma `4902:19649`.
 * Same sticky Capture materials panel as Product; intro copy sits above the list.
 */
export function HomeQrMaterials() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="home-qr-materials-heading"
    >
      <div
        className={cn(
          "mx-auto w-full py-12.5 lg:py-[70px]",
          marketingChromeContentInset,
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
