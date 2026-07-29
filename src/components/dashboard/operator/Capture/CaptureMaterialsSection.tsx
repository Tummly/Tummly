import { ShoppingBag } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  CAPTURE_MATERIALS_ACTIONS_CLASS,
  CAPTURE_MATERIALS_INNER_CARD_CLASS,
  CAPTURE_MATERIALS_INNER_COPY_CLASS,
  CAPTURE_MATERIALS_INNER_HELPER_CLASS,
  CAPTURE_MATERIALS_INNER_TITLE_CLASS,
  CAPTURE_SECTION_CLASS,
  CAPTURE_SECTION_SUBTITLE_CLASS,
  CAPTURE_SECTION_TITLE_CLASS,
  OPERATOR_CAPTURE_SECTION_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import { PERFORMANCE_HEADER_COPY_CLASS } from "@/lib/operatorHome/performanceOverviewPresentation"

/** QR materials section — presentational chrome only (no shop fulfillment). */
export function CaptureMaterialsSection() {
  const copy = OPERATOR_CAPTURE_SECTION_COPY.materials

  return (
    <section className={CAPTURE_SECTION_CLASS}>
      <div className="flex flex-col gap-10">
        <header className={PERFORMANCE_HEADER_COPY_CLASS}>
          <div className="leading-[0]">
            <h2 className={CAPTURE_SECTION_TITLE_CLASS}>{copy.title}</h2>
          </div>
          <div className="leading-[0]">
            <p className={CAPTURE_SECTION_SUBTITLE_CLASS}>{copy.description}</p>
          </div>
        </header>

        <div className={CAPTURE_MATERIALS_INNER_CARD_CLASS}>
          <div className={CAPTURE_MATERIALS_INNER_COPY_CLASS}>
            <p className={CAPTURE_MATERIALS_INNER_TITLE_CLASS}>
              {copy.printedTitle}
            </p>
            <p className={CAPTURE_MATERIALS_INNER_HELPER_CLASS}>
              {copy.printedHelper}
            </p>
          </div>
          <div className={CAPTURE_MATERIALS_ACTIONS_CLASS}>
            <Button type="button" variant="op-primary" disabled>
              <ShoppingBag className="size-4" aria-hidden />
              {copy.orderCta}
            </Button>
            <Button type="button" variant="op-secondary" disabled>
              {copy.viewOrdersCta}
            </Button>
          </div>
        </div>
      </div>
    </section>
  )
}
