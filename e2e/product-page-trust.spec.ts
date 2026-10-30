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

test("Certification carousel scrolls with the arrow buttons", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await openProductPage(page)
  const prev = page.getByRole("button", { name: "Previous certifications" })
  const next = page.getByRole("button", { name: "Next certifications" })
  await next.scrollIntoViewIfNeeded()

  // The ends use aria-disabled (not disabled) so focus stays on the button.
  await expect(prev).toHaveAttribute("aria-disabled", "true")
  await expect(next).toHaveAttribute("aria-disabled", "false")
  await next.click()
  await expect(prev).toHaveAttribute("aria-disabled", "false")
  await prev.click()
  await expect(prev).toHaveAttribute("aria-disabled", "true")
})
