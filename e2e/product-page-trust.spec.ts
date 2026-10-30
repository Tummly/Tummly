import { expect, test } from "@playwright/test"

import { openProductPage, section } from "./product-page"

test("Trust accordion opens and closes each row", async ({ page }) => {
  await openProductPage(page)
  const trust = section(page, /Guest trust\./)
  const row = trust.getByRole("button", { name: "A private way to be heard" })

  await expect(row).toHaveAttribute("aria-expanded", "true")
  await expect(trust.getByText("The customer’s feedback goes to your restaurant team.")).toBeVisible()
  await row.click()
  await expect(row).toHaveAttribute("aria-expanded", "false")
  await expect(trust.getByText("The customer’s feedback goes to your restaurant team.")).toBeHidden()
  await row.click()
  await expect(row).toHaveAttribute("aria-expanded", "true")
})

// ENG01 / E3: certification badges stay hidden until the Director approves each
// claim (`publicTrustClaims.ts`). The carousel itself is covered by
// `MarketingCertificationCarousel.test.tsx`.
test("Trust section withholds the certification badges and arrows", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await openProductPage(page)
  const trust = section(page, /Guest trust\./)

  await expect(trust.getByRole("button", { name: "A private way to be heard" })).toBeVisible()
  await expect(page.getByRole("button", { name: "Previous certifications" })).toHaveCount(0)
  await expect(page.getByRole("button", { name: "Next certifications" })).toHaveCount(0)
  await expect(page.getByText("ISO/IEC 27001")).toHaveCount(0)
})

test("For Groups hero withholds the trust strip", async ({ page }) => {
  await page.goto("/for-groups")
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible()
  await expect(page.getByText("Built with trust at the heart of Tummly.")).toHaveCount(0)
  await expect(page.getByRole("button", { name: "Next certifications" })).toHaveCount(0)
})
