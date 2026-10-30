import { renderToStaticMarkup } from "react-dom/server"
import { MemoryRouter } from "react-router-dom"
import { describe, expect, it } from "vitest"

import type { PublicTrustIndicator } from "@/lib/marketing/publicTrustIndicators"

import { TrustIndicatorTile, TrustIndicatorTooltipBody } from "./TrustIndicatorTile"

const ROADMAP: PublicTrustIndicator = {
  itemNumber: 2,
  title: "ISO/IEC 27001",
  status: "Roadmap",
  statusLabel: "On our roadmap",
  iconKey: "roadmap-iso-27001",
  imageUrl: null,
  altText: "",
  tooltip: "We are working towards this standard.",
  cta: { type: "ExternalProof", label: "View record", href: "https://proof.example/record" },
}

const VERIFIED_EXTERNAL: PublicTrustIndicator = {
  itemNumber: 1,
  title: "ICO data protection fee",
  status: "Verified",
  statusLabel: "Verified",
  iconKey: "ico-data",
  imageUrl: null,
  altText: "",
  tooltip: "We pay the data protection fee.",
  cta: { type: "ExternalProof", label: "View ICO record", href: "https://ico.example/record" },
}

function tile(
  indicator: PublicTrustIndicator = ROADMAP,
  variant: "footer" | "carousel" = "footer",
) {
  return renderToStaticMarkup(<TrustIndicatorTile indicator={indicator} variant={variant} />)
}

function body(indicator: PublicTrustIndicator) {
  return renderToStaticMarkup(
    <MemoryRouter>
      <TrustIndicatorTooltipBody indicator={indicator} />
    </MemoryRouter>,
  )
}

describe("TrustIndicatorTile (closed)", () => {
  it("renders the title and the status text", () => {
    const html = tile()
    expect(html).toContain("ISO/IEC 27001")
    expect(html).toContain("On our roadmap")
  })

  it("names the trigger with the title and the status label", () => {
    expect(tile()).toContain('aria-label="ISO/IEC 27001, On our roadmap"')
  })

  it("describes the trigger with an sr-only copy of the tooltip text", () => {
    const html = tile(VERIFIED_EXTERNAL)
    const id = html.match(/<button[^>]*aria-describedby="([^"]+)"/)?.[1] ?? ""
    expect(id).not.toBe("")
    expect(html).toContain(
      `<span id="${id}" class="sr-only">We pay the data protection fee.</span>`,
    )
  })

  it("has no description attribute or element when the tooltip is empty", () => {
    const html = tile({ ...ROADMAP, tooltip: "" })
    expect(html).not.toContain("aria-describedby")
    expect(html).not.toContain('class="sr-only">')
  })

  it("renders no link while closed, even with a CTA injected", () => {
    expect(tile()).not.toContain("<a")
  })

  it("draws the Figma 0.825px border in #b3b3b3", () => {
    expect(tile()).toContain("border-[0.825px]")
    expect(tile()).toContain("border-[#b3b3b3]")
  })

  it("sizes the arrow anchor to 42px at the tile's left edge, so its centre is 21px in", () => {
    const html = tile()
    expect(html).toContain("w-[42px]")
    expect(html).toContain("left-0")
    expect(html).not.toContain("left-[21px]")
  })

  it("is a button with type button", () => {
    expect(tile()).toMatch(/<button[^>]*type="button"/)
  })

  it("uses the 94px footer width and fills the slot in the carousel", () => {
    expect(tile(ROADMAP, "footer")).toContain("w-[94px]")
    expect(tile(ROADMAP, "carousel")).toContain("w-full")
    expect(tile(ROADMAP, "carousel")).not.toContain("w-[120px]")
    expect(tile(ROADMAP, "carousel")).not.toContain("w-[94px]")
  })

  it("never draws artwork for a Roadmap item, even with an artwork key", () => {
    expect(tile({ ...ROADMAP, iconKey: "ico-data" })).not.toContain("<img")
    expect(tile({ ...ROADMAP, iconKey: "ico-data" })).toMatch(/lucide-shield[\s"]/)
  })

  it("uses the small icon box in the footer and the large one in the carousel", () => {
    expect(tile(ROADMAP, "footer")).toContain("size-[43px]")
    expect(tile(ROADMAP, "carousel")).toContain("size-[74px]")
  })
})

describe("TrustIndicatorTooltipBody", () => {
  it("renders the tooltip text", () => {
    expect(body(ROADMAP)).toContain("We are working towards this standard.")
  })

  it("renders no CTA for a Roadmap item even when one is injected", () => {
    expect(body(ROADMAP)).not.toContain("<a")
  })

  it("renders no CTA for a Verified item without one", () => {
    expect(body({ ...VERIFIED_EXTERNAL, cta: null })).not.toContain("<a")
  })

  it("renders an external proof link in a new tab with hidden new-tab text", () => {
    const html = body(VERIFIED_EXTERNAL)
    expect(html).toContain('href="https://ico.example/record"')
    expect(html).toContain('target="_blank"')
    expect(html).toContain('rel="noopener noreferrer"')
    expect(html).toContain("View ICO record")
    expect(html).toContain('class="sr-only"')
    expect(html).toContain("(opens in a new tab)")
    expect(html).toMatch(/lucide-external-link[\s"]/)
  })

  it("renders an internal page CTA as a router link", () => {
    const html = body({
      ...VERIFIED_EXTERNAL,
      cta: { type: "InternalPage", label: "Read our policy", href: "/trust-privacy" },
    })
    expect(html).toContain('href="/trust-privacy"')
    expect(html).toContain("Read our policy")
    expect(html).not.toContain('target="_blank"')
  })

  it("renders a cookie settings CTA as a button, not a link", () => {
    const html = body({
      ...VERIFIED_EXTERNAL,
      cta: { type: "CookieSettings", label: "Manage cookies", href: null },
    })
    expect(html).toContain("Manage cookies")
    expect(html).toMatch(/<button[^>]*type="button"/)
    expect(html).not.toContain("<a")
  })
})
