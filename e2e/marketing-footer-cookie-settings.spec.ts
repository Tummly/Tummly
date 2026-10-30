import { expect, test } from "@playwright/test"

import { openFooter } from "./marketing-footer"

test("Marketing footer Cookie settings opens the cookie dialog", async ({ page }) => {
  const footer = await openFooter(page)
  await footer
    .getByRole("navigation", { name: "Company & Legal" })
    .getByRole("button", { name: "Cookie settings" })
    .click()
  await expect(page.getByRole("dialog", { name: "Cookie settings" })).toBeVisible()
})
