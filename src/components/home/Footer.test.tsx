import { renderToStaticMarkup } from "react-dom/server"
import { MemoryRouter } from "react-router-dom"
import { describe, expect, it } from "vitest"

import { FooterPilotButton, TrustIndicatorGrid } from "@/components/home/Footer"
import type { PublicTrustIndicator } from "@/lib/marketing/publicTrustIndicators"

const INDICATORS: PublicTrustIndicator[] = [
  {
    itemNumber: 1,
    title: "ICO data protection fee",
    status: "Verified",
    statusLabel: "Verified",
    iconKey: "ico-data",
    imageUrl: null,
    altText: "",
    tooltip: "We pay the data protection fee.",
    cta: null,
  },
  {
    itemNumber: 2,
    title: "ISO/IEC 27001",
    status: "Roadmap",
    statusLabel: "On our roadmap",
    iconKey: "roadmap-iso-27001",
    imageUrl: null,
    altText: "",
    tooltip: "We are working towards this standard.",
    cta: null,
  },
]

function gridHtml(items: PublicTrustIndicator[] = INDICATORS) {
  return renderToStaticMarkup(<TrustIndicatorGrid items={items} />)
}

describe("TrustIndicatorGrid", () => {
  it("renders the heading above a labelled list", () => {
    const html = gridHtml()
    expect(html).toContain("Built with trust in mind")
    expect(html).toContain('<ul aria-label="Trust indicators"')
  })

  it("renders one list item per indicator, in the order given", () => {
    const html = gridHtml()
    expect(html.match(/<li/g)).toHaveLength(2)
    expect(html.indexOf("ICO data protection fee")).toBeLessThan(
      html.indexOf("ISO/IEC 27001"),
    )
  })

  it("shows the status text on each tile", () => {
    const html = gridHtml()
    expect(html).toContain("Verified")
    expect(html).toContain("On our roadmap")
  })

  it("uses 2, 3 and 5 columns for the small, medium and desktop widths", () => {
    const html = gridHtml()
    expect(html).toContain("grid-cols-2")
    expect(html).toContain("min-[360px]:grid-cols-3")
    expect(html).toContain("sm:grid-cols-5")
  })
})

describe("FooterPilotButton", () => {
  it("is the Pilot signup link", () => {
    const html = renderToStaticMarkup(
      <MemoryRouter>
        <FooterPilotButton />
      </MemoryRouter>,
    )
    expect(html).toContain('href="/signup?plan=Pilot"')
    expect(html).toContain("Start 30-day Pilot")
  })
})
