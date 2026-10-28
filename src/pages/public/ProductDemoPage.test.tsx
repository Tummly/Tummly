import { renderToStaticMarkup } from "react-dom/server"
import { MemoryRouter } from "react-router-dom"
import { afterEach, describe, expect, it, vi } from "vitest"

import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"
import ProductDemoPage from "@/pages/public/ProductDemoPage"

function renderPage() {
  return renderToStaticMarkup(
    <MemoryRouter initialEntries={["/product-demo"]}>
      <ProductDemoPage />
    </MemoryRouter>
  )
}

describe("ProductDemoPage", () => {
  afterEach(() => {
    vi.unstubAllEnvs()
  })

  it("renders the unavailable state straight away when the demo flag is off", () => {
    vi.stubEnv("VITE_PRODUCT_DEMO_ENABLED", "")

    const html = renderPage()

    expect(html).toContain(PRODUCT_DEMO.entry.unavailableTitle)
    expect(html).toContain('id="demo-entry-unavailable-title"')
    expect(html).not.toContain('role="status"')
  })

  it("shows the loading shell while the demo boots when the flag is on", () => {
    vi.stubEnv("VITE_PRODUCT_DEMO_ENABLED", "true")

    const html = renderPage()

    expect(html).toContain('role="status"')
    expect(html).not.toContain(PRODUCT_DEMO.entry.unavailableTitle)
  })
})
