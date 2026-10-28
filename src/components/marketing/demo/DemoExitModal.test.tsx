import { renderToStaticMarkup } from "react-dom/server"
import { MemoryRouter } from "react-router-dom"
import { describe, expect, it } from "vitest"

import { DemoExitModal } from "@/components/marketing/demo/DemoExitModal"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"

describe("DemoExitModal", () => {
  it("offers only the conversion and chooser actions, not free exploring", () => {
    const html = renderToStaticMarkup(
      <MemoryRouter>
        <DemoExitModal onLeaveDemo={() => {}} />
      </MemoryRouter>
    )

    expect(html).toContain(PRODUCT_DEMO.exit.primaryLabel)
    expect(html).toContain(PRODUCT_DEMO.exit.secondaryLabel)
    expect(html).not.toContain("Continue exploring the demo")
    expect(html).not.toContain("<button")
  })
})
