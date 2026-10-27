import { readFileSync } from "node:fs"
import { dirname, join } from "node:path"
import { fileURLToPath } from "node:url"
import { describe, expect, it } from "vitest"

import { SHOP_BODY_TEXT_CLASS } from "@/lib/operatorShop/shopSurfacePresentation"

const cssPath = join(dirname(fileURLToPath(import.meta.url)), "../../index.css")
const css = readFileSync(cssPath, "utf8")

/** Body text block for the given selector, e.g. "html.op.dark". */
function blockFor(selector: string): string {
  const start = css.indexOf(`\n${selector} {`)
  expect(start, `${selector} block`).toBeGreaterThan(-1)
  return css.slice(start, css.indexOf("\n}", start))
}

describe("mode-aware Shop body text token", () => {
  it("uses the Shop body class", () => {
    expect(SHOP_BODY_TEXT_CLASS).toBe("text-op-text-body")
  })

  it("exposes the token to Tailwind as text-op-text-body", () => {
    expect(css).toContain("--color-op-text-body: var(--op-text-body);")
  })

  it("is gray-650 in light mode", () => {
    expect(blockFor("html.op")).toContain(
      "--op-text-body: var(--op-color-gray-650);"
    )
  })

  it("is gray-500 in dark mode", () => {
    expect(blockFor("html.op.dark")).toContain(
      "--op-text-body: var(--op-color-gray-500);"
    )
  })
})
