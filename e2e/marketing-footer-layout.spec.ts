import { expect, test, type Locator } from "@playwright/test"

import { openFooter } from "./marketing-footer"

async function box(locator: Locator) {
  const b = await locator.boundingBox()
  if (!b) throw new Error("element has no box")
  return b
}

test.describe("Marketing footer layout", () => {
  test("desktop matches Figma 5839:73487 at its 1688px frame width", async ({ page }) => {
    await page.setViewportSize({ width: 1688, height: 1000 })
    const footer = await openFooter(page)

    const nav = (name: string) => footer.getByRole("navigation", { name, exact: true })
    const product = await box(nav("Product"))
    const restaurants = await box(nav("Restaurants"))
    const social = await box(nav("Find us on social"))
    const company = await box(nav("Company"))

    // 178px columns, 100px gaps, rows 70px apart.
    expect(restaurants.x - product.x).toBe(278)
    expect(social.x - product.x).toBe(556)
    expect(Math.round(company.y - product.y)).toBe(272)

    const askAi = footer.getByRole("list", { name: "Ask AI about Tummly" })
    const tile = askAi.getByRole("link").first()
    expect(await box(tile)).toMatchObject({ width: 28, height: 28 })
    await expect(tile.locator("div")).toHaveCSS("background-color", "rgb(20, 20, 20)")

    await expect(footer.getByText("Turn everyday orders and visits")).toHaveCSS("font-size", "20px")
  })

  test("mobile matches Figma 6243:19686 at 375px", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 800 })
    const footer = await openFooter(page)

    const askAi = footer.getByRole("list", { name: "Ask AI about Tummly" })
    await expect(askAi).toHaveCount(1)
    expect((await box(askAi)).y).toBeLessThan(
      (await box(footer.getByRole("navigation", { name: "Product" }))).y
    )
    expect(await box(askAi.getByRole("link").first())).toMatchObject({ width: 26, height: 26 })

    await expect(footer.getByText("Turn everyday orders and visits")).toHaveCSS("font-size", "18px")
    await expect(footer.getByText("Ask AI about Tummly", { exact: true }).filter({ visible: true })).toHaveCSS("font-size", "16px")
    await expect(footer.getByText("Proud to be a company of")).toHaveCSS("font-size", "12px")
  })

  for (const width of [768, 1024, 1440]) {
    test(`link columns do not overlap at ${width}px`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 })
      const footer = await openFooter(page)

      const legalLinks = footer.getByRole("navigation", { name: "Company & Legal" }).getByRole("link")
      const widest = await legalLinks.evaluateAll((links) =>
        Math.max(...links.map((link) => link.getBoundingClientRect().right))
      )
      const askAiHeading = await box(footer.getByText("Ask AI about Tummly", { exact: true }).filter({ visible: true }))
      expect(widest).toBeLessThan(askAiHeading.x)

      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth
      )
      expect(overflow).toBe(0)
    })
  }
})
