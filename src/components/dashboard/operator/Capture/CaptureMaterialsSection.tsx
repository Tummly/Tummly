import { ShoppingBag } from "lucide-react"
import { Link, useOutletContext } from "react-router-dom"

import { Button } from "@/components/ui/button"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
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
import { operatorDashboardNavPath } from "@/lib/operatorHome/operatorDashboardPaths"

/**
 * QR materials section — Order print materials opens Shop; View orders opens
 * Shop Orders for the selected location.
 */
export function CaptureMaterialsSection() {
  const copy = OPERATOR_CAPTURE_SECTION_COPY.materials
  const { mode, selectedLocationId } =
    useOutletContext<DashboardOutletContext>()
  const shopPath = operatorDashboardNavPath(
    mode,
    "tummly-shop",
    selectedLocationId
  )
  const shopOrdersPath = `${shopPath}&view=orders`

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
            <Button type="button" variant="op-primary" asChild>
              <Link to={shopPath}>
                <ShoppingBag className="size-4" aria-hidden />
                {copy.orderCta}
              </Link>
            </Button>
            <Button type="button" variant="op-secondary" asChild>
              <Link to={shopOrdersPath}>{copy.viewOrdersCta}</Link>
            </Button>
          </div>
        </div>
      </div>
    </section>
  )
}
