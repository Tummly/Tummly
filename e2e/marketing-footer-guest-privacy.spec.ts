import { expect, test } from "@playwright/test"

import { openFooter } from "./marketing-footer"

test("Marketing footer Privacy for Guests opens the guest section of the Privacy Notice", async ({ page }) => {
  const footer = await openFooter(page)
  await footer.getByRole("link", { name: "Privacy for Guests" }).click()

  await expect(page).toHaveURL(/\/privacy#smart-guest-links-guest-feedback-and-restaurant-follow-up$/)
  await expect(
    page.getByRole("heading", { name: "10. Smart Guest Links, guest feedback and restaurant follow-up" })
  ).toBeInViewport()
})
