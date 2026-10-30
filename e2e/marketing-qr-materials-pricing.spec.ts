import { expect, test } from "@playwright/test"

import { openQrMaterials, sectionOf } from "./marketing-qr-materials"

test("plan cards show prices and plan links (Figma 6859:12685)", async ({ page }) => {
  await openQrMaterials(page)
  const section = sectionOf(page, /A plan that fits your restaurant/)
  await section.scrollIntoViewIfNeeded()
  for (const price of ["£0", "£39", "£99", "£199"]) {
    await expect(section.getByText(price, { exact: true })).toBeVisible()
  }
  await expect(section.getByRole("link", { name: "Choose Starter" })).toHaveAttribute("href", /\/signup\?plan=Starter/)
  await expect(section.getByRole("link", { name: "Choose Growth" })).toHaveAttribute("href", /\/signup\?plan=Growth/)
  await expect(section.getByRole("link", { name: "Choose Group" })).toHaveAttribute("href", /\/signup\?plan=Group/)
  await expect(section.getByRole("link", { name: "Compare all plans" })).toHaveAttribute("href", "/pricing")
})

test("price rows share one line at desktop widths (Figma 6859:12685)", async ({ page }) => {
  for (const width of [1024, 1440]) {
    await page.setViewportSize({ width, height: 900 })
    await openQrMaterials(page)
    const section = sectionOf(page, /A plan that fits your restaurant/)
    await section.scrollIntoViewIfNeeded()
    await expect(section.getByText("£199", { exact: true })).toBeVisible()
    const tops = await Promise.all(
      ["£0", "£39", "£99", "£199"].map(async (p) => (await section.getByText(p, { exact: true }).boundingBox())!.y),
    )
    expect(Math.max(...tops) - Math.min(...tops), `width ${width}: ${tops.join(", ")}`).toBeLessThanOrEqual(1)
  }
})

test("pricing quote error offers a retry", async ({ page }) => {
  await page.route("**/commercial-catalog/custom-unit-rates", (route) => route.fulfill({ status: 500, body: "{}" }))
  await openQrMaterials(page)
  const section = sectionOf(page, /A plan that fits your restaurant/)
  await section.scrollIntoViewIfNeeded()
  await expect(section.getByText(/could not load a current quote/i)).toBeVisible()
  await expect(section.getByRole("button", { name: "Try again" })).toBeVisible()
})
