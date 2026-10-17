import { ProductCaptureMaterialsPanel } from "@/components/marketing/product/ProductCaptureMaterialsPanel"
import {
  FOR_GROUPS_QR_MATERIALS,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import { cn } from "@/lib/utils"

/**
 * Figma For Groups QR materials `5661:20711`.
 * Reuses Product Capture materials panel; intro copy is group-specific.
 */
export function ForGroupsQrMaterials() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="for-groups-qr-materials-heading"
    >
      <div className={cn("mx-auto w-full", FOR_GROUPS_SECTION_INSET, FOR_GROUPS_SECTION_Y)}>
        <ProductCaptureMaterialsPanel
          intro={{
            eyebrow: FOR_GROUPS_QR_MATERIALS.eyebrow,
            title: FOR_GROUPS_QR_MATERIALS.title,
            body: FOR_GROUPS_QR_MATERIALS.body,
            titleId: "for-groups-qr-materials-heading",
          }}
        />
      </div>
    </section>
  )
}
