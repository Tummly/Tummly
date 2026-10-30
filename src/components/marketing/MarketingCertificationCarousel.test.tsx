import { renderToStaticMarkup } from "react-dom/server"
import { describe, expect, it } from "vitest"

import { MarketingCertificationCarousel } from "@/components/marketing/MarketingCertificationCarousel"
import { PRODUCT_TRUST } from "@/content/marketing/productPage"

function renderHtml() {
  return renderToStaticMarkup(
    <MarketingCertificationCarousel
      certifications={PRODUCT_TRUST.certifications}
    />,
  )
}

/** `renderToStaticMarkup` escapes text the same way React does for the DOM. */
function escapeHtml(text: string) {
  return text
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#x27;")
}

function buttonTag(html: string, label: string) {
  const match = html.match(
    new RegExp(`<button[^>]*aria-label="${label}"[^>]*>`),
  )
  expect(match).not.toBeNull()
  return match![0]
}

describe("MarketingCertificationCarousel", () => {
  it("lists every certification label", () => {
    const html = renderHtml()
    for (const cert of PRODUCT_TRUST.certifications) {
      expect(html).toContain(`>${escapeHtml(cert.label)}</p>`)
    }
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
