import { renderToStaticMarkup } from "react-dom/server"
import { describe, expect, it } from "vitest"

import icoDataUrl from "@/assets/svg/marketing/footer-trust-badges/ico-data.svg"

import type { PublicTrustStatus } from "@/lib/marketing/publicTrustIndicators"

import { TrustIndicatorIcon } from "./TrustIndicatorIcon"

function render(
  overrides: {
    iconKey?: string | null
    imageUrl?: string | null
    size?: "sm" | "lg"
    status?: PublicTrustStatus
  } = {},
) {
  return renderToStaticMarkup(
    <TrustIndicatorIcon
      iconKey={null}
      imageUrl={null}
      altText=""
      size="sm"
      status="Verified"
      {...overrides}
    />,
  )
}

describe("TrustIndicatorIcon", () => {
  it("renders a Lucide SVG for a roadmap key", () => {
    const html = render({ iconKey: "roadmap-iso-27001" })
    expect(html).toContain("<svg")
    expect(html).toMatch(/lucide-file-lock[\s"]/)
    expect(html).not.toContain("<img")
  })

  it("renders the imported footer artwork URL for an ICO key", () => {
    const html = render({ iconKey: "ico-data" })
    expect(html).toContain("<img")
    expect(html).toContain(`src="${icoDataUrl}"`)
  })

  it("falls back to Lucide Shield for an unknown key", () => {
    const html = render({ iconKey: "not-a-key" })
    expect(html).toMatch(/lucide-shield[\s"]/)
    expect(html).not.toContain("<img")
  })

  it("falls back to Lucide Shield for a null key", () => {
    expect(render({ iconKey: null })).toMatch(/lucide-shield[\s"]/)
  })

  it("does not treat an inherited Object key as artwork", () => {
    const html = render({ iconKey: "constructor" })
    expect(html).toMatch(/lucide-shield[\s"]/)
    expect(html).not.toContain("<img")
  })

  it("renders imageUrl as an image and wins over iconKey", () => {
    const html = render({ iconKey: "ico-data", imageUrl: "https://cdn.example/logo.svg" })
    expect(html).toContain('src="https://cdn.example/logo.svg"')
    expect(html).not.toContain(icoDataUrl)
    expect(html).not.toMatch(/lucide-shield[\s"]/)
    expect(html).toContain('alt=""')
    expect(html).toContain('loading="lazy"')
    expect(html).toContain('decoding="async"')
  })

  it("sizes the tile box at 43px for sm and 74px for lg", () => {
    expect(render({ iconKey: "ico-data", size: "sm" })).toContain("size-[43px]")
    expect(render({ iconKey: "ico-data", size: "lg" })).toContain("size-[74px]")
    expect(render({ iconKey: "ico-data", size: "lg" })).not.toContain("size-[43px]")
  })

  it("keeps the image at natural size inside the box, with no forced width or height", () => {
    const sm = render({ imageUrl: "/a.svg", size: "sm" })
    expect(sm).toContain("size-[43px]")
    const smImg = sm.match(/<img[^>]*>/)?.[0] ?? ""
    expect(smImg).toContain("max-h-full max-w-full object-contain")
    expect(smImg).not.toMatch(/\swidth=/)
    expect(smImg).not.toMatch(/\sheight=/)

    const lg = render({ imageUrl: "/a.svg", size: "lg" })
    expect(lg).toContain("size-[74px]")
    const lgImg = lg.match(/<img[^>]*>/)?.[0] ?? ""
    expect(lgImg).toContain("max-h-full max-w-full object-contain")
    expect(lgImg).not.toMatch(/\swidth=/)
    expect(lgImg).not.toMatch(/\sheight=/)
  })

  it("draws only the neutral glyph for a Roadmap item, even with an artwork key", () => {
    const html = render({ iconKey: "ico-data", status: "Roadmap" })
    expect(html).toMatch(/lucide-shield[\s"]/)
    expect(html).not.toContain("<img")
    expect(html).not.toContain(icoDataUrl)
  })

  it("draws the neutral roadmap glyph for a Roadmap key, and ignores any image", () => {
    const html = render({
      iconKey: "roadmap-iso-27001",
      imageUrl: "https://cdn.example/award.svg",
      status: "Roadmap",
    })
    expect(html).toMatch(/lucide-file-lock[\s"]/)
    expect(html).not.toContain("<img")
  })

  it("keeps the artwork for a Verified item with the same key", () => {
    const html = render({ iconKey: "ico-data", status: "Verified" })
    expect(html).toContain(`src="${icoDataUrl}"`)
  })

  it("is always hidden from assistive technology", () => {
    for (const iconKey of ["roadmap-gbc", "ico-dpe", "missing", null]) {
      expect(render({ iconKey })).toContain('aria-hidden="true"')
    }
    expect(render({ imageUrl: "/a.svg" })).toContain('aria-hidden="true"')
  })
})
