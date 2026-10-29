import { expect, test } from "@playwright/test"

import { openProductPage } from "./product-page"

for (const width of [375, 768, 1024, 1440]) {
  test(`Product page has no horizontal overflow at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 })
    await openProductPage(page)
    await page.locator("footer").last().scrollIntoViewIfNeeded()
    const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth)
    expect(scrollWidth).toBeLessThanOrEqual(width)
  })
}

test("Product page shows its copy while images still load", async ({ page }) => {
  await page.route(/\/assets\/images\/marketing\/product\//, async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 4000))
    await route.continue()
  })
  await openProductPage(page)
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible({ timeout: 3000 })
  await expect(page.getByRole("link", { name: "Get started" }).first()).toBeVisible()
  await expect(page.getByRole("img").first()).toBeAttached()
})
