import { renderToStaticMarkup } from "react-dom/server"
import { describe, expect, it } from "vitest"

import { TrustIndicatorIcon } from "./TrustIndicatorIcon"

describe("TrustIndicatorIcon", () => {
  it("renders a Lucide SVG for a roadmap key", () => {
    const html = renderToStaticMarkup(<TrustIndicatorIcon iconKey="roadmap-iso-27001" />)
    expect(html).toContain("<svg")
    expect(html).toMatch(/lucide-file-lock[\s"]/)
    expect(html).not.toContain("<img")
  })

  it("renders the footer SVG artwork for an ICO key", () => {
    const html = renderToStaticMarkup(<TrustIndicatorIcon iconKey="ico-data" />)
    expect(html).toContain("<img")
    expect(html).toContain("ico-data")
  })

  it("falls back to Lucide Shield for an unknown key", () => {
    const html = renderToStaticMarkup(<TrustIndicatorIcon iconKey="not-a-key" />)
    expect(html).toMatch(/lucide-shield[\s"]/)
    expect(html).not.toContain("<img")
  })

  it("falls back to Lucide Shield for a null key", () => {
    const html = renderToStaticMarkup(<TrustIndicatorIcon iconKey={null} />)
    expect(html).toMatch(/lucide-shield[\s"]/)
  })

  it("is always hidden from assistive technology", () => {
    for (const iconKey of ["roadmap-gbc", "ico-dpe", "missing", null]) {
      const html = renderToStaticMarkup(<TrustIndicatorIcon iconKey={iconKey} />)
      expect(html).toContain('aria-hidden="true"')
    }
  })
})
