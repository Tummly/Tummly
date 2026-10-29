import { expect, test } from "@playwright/test"

import { openProductPage, section } from "./product-page"

test("Bottom sign-up CTA validates the email and continues to signup", async ({ page }) => {
  await openProductPage(page)
  const cta = section(page, "Put the Guest Loop to work in your restaurant.")
  const email = cta.getByLabel(/email/i)

  await expect(cta.getByRole("button", { name: "Continue with Google" })).toBeVisible()
  await expect(cta.getByRole("button", { name: "Continue with Microsoft" })).toBeVisible()
  await expect(cta.getByRole("link", { name: "Terms of Service" })).toHaveAttribute("href", "/terms")
  await expect(cta.getByRole("link", { name: "Privacy Notice" })).toHaveAttribute("href", "/privacy")
  await expect(cta.getByText("You’ll review and accept the required terms during account setup.", { exact: false })).toBeVisible()

  await email.fill("not-an-email")
  await cta.getByRole("button", { name: "Get started" }).click()
  await expect(page).toHaveURL(/\/product$/)
  expect(await email.evaluate((el: HTMLInputElement) => el.validity.valid)).toBe(false)

  await email.fill("owner@example.com")
  await cta.getByRole("button", { name: "Get started" }).click()
  await expect(page).toHaveURL(/\/signup\?.*plan=Pilot/)
})
