import { renderToStaticMarkup } from "react-dom/server"
import { describe, expect, it } from "vitest"

import { MarketingCertificationCarousel } from "@/components/marketing/MarketingCertificationCarousel"
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

function renderHtml() {
  return renderToStaticMarkup(
    <MarketingCertificationCarousel indicators={INDICATORS} />,
  )
}

function buttonTag(html: string, label: string) {
  const match = html.match(
    new RegExp(`<button[^>]*aria-label="${label}"[^>]*>`),
  )
  expect(match).not.toBeNull()
  return match![0]
}

describe("MarketingCertificationCarousel", () => {
  it("renders a tile per indicator, named by title and status", () => {
    const html = renderHtml()
    expect(html).toContain('aria-label="ICO data protection fee, Verified"')
    expect(html).toContain('aria-label="ISO/IEC 27001, On our roadmap"')
  })

  it("gives each tile the existing slot width", () => {
    const html = renderHtml()
    expect(html).toContain("w-[clamp(140px,10.3vw,172px)]")
  })

  it("keeps the indicator order", () => {
    const html = renderHtml()
    expect(html.indexOf("ICO data protection fee")).toBeLessThan(
      html.indexOf("ISO/IEC 27001"),
    )
  })

  it("holds Previous at the start and keeps Next active", () => {
    const html = renderHtml()
    const prev = buttonTag(html, "Previous certifications")
    const next = buttonTag(html, "Next certifications")
    expect(prev).toContain('aria-disabled="true"')
    expect(next).toContain('aria-disabled="false"')
    // aria-disabled, not disabled: focus can stay on the button.
    expect(prev).not.toMatch(/\sdisabled(=|\s|>)/)
  })
})
