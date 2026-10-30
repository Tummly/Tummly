import { HomeQrMaterials } from "@/components/home/HomeQrMaterials"
import { QR_MATERIALS_CATALOGUE } from "@/content/marketing/qrMaterialsPage"

/**
 * Figma QR materials catalogue `5661:19364`.
 * Same Capture materials panel as Home QR materials (`4902:19649`) with the
 * QR page intro and no eyebrow. The steps section below supplies the 70px gap.
 */
export function QrMaterialsCatalogue() {
  return (
    <HomeQrMaterials
      content={{
        title: QR_MATERIALS_CATALOGUE.title,
        body: QR_MATERIALS_CATALOGUE.body,
      }}
      className="lg:pb-0"
    />
  )
}
